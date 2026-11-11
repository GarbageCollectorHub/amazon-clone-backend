using System.Net;
using System.Reflection;
using System.Text.Json;
using AMZN.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AMZN.Tests;

public class UploadRequestSizeTests
{
    private const int RequestLimitBytes = 25 * 1024 * 1024;
    private const int FileLimitBytes = 5 * 1024 * 1024;

    // Отправляем настоящую multipart-форму на Kestrel: размер включает файлы, заголовки частей и разделители.
    // Ровно 25 МиБ и на байт меньше должны пройти, на байт больше - отклониться до вызова действия контроллера.
    // Ограничения копируем с каждого настоящего POST-метода; бизнес-код заменён тестовой формой без SQL и Blob.
    [Theory]
    [InlineData(typeof(AdminProductsController), "Create", -1)]
    [InlineData(typeof(AdminProductsController), "Create", 0)]
    [InlineData(typeof(AdminProductsController), "Create", 1)]
    [InlineData(typeof(AdminProductsController), "Edit", -1)]
    [InlineData(typeof(AdminProductsController), "Edit", 0)]
    [InlineData(typeof(AdminProductsController), "Edit", 1)]
    [InlineData(typeof(AdminController), "AddCategory", -1)]
    [InlineData(typeof(AdminController), "AddCategory", 0)]
    [InlineData(typeof(AdminController), "AddCategory", 1)]
    public async Task MultipartRequest_RespectsTotalSizeLimit(Type controller, string action, int bytesAboveLimit)
    {
        var sourceAction = controller.GetMethods().Single(method => method.Name == action && method.IsDefined(typeof(HttpPostAttribute)));
        var requestLimit = sourceAction.GetCustomAttribute<RequestSizeLimitAttribute>();
        var formLimit = sourceAction.GetCustomAttribute<RequestFormLimitsAttribute>();
        Assert.NotNull(requestLimit);
        Assert.NotNull(formLimit);
        var state = new UploadSizeProbeState();

        await using var host = await LocalHttpTestHost.StartAsync(services =>
        {
            services.AddSingleton(state);
            services.AddControllers(options =>
            {
                options.Filters.Add(requestLimit);
                options.Filters.Add(formLimit);
            }).AddApplicationPart(typeof(UploadSizeProbeController).Assembly);
        }, app =>
        {
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        });

        using var form = CreateFormWithTotalSize(RequestLimitBytes + bytesAboveLimit);
        using var request = new HttpRequestMessage(HttpMethod.Post, "upload-size-probe") { Content = form };
        // Сначала даём серверу проверить Content-Length. Иначе он может закрыть соединение,
        // пока клиент ещё отправляет уже отклонённые 25 МиБ, и HTTP-статус потеряется за ошибкой записи сокета.
        request.Headers.ExpectContinue = true;
        using var response = await host.Client.SendAsync(request);

        if (bytesAboveLimit > 0)
        {
            // Kestrel может вернуть 413 напрямую, а MVC - преобразовать ошибку чтения формы в 400.
            Assert.Contains(response.StatusCode, new[] { HttpStatusCode.RequestEntityTooLarge, HttpStatusCode.BadRequest });
            Assert.Equal(0, state.AcceptedForms);
        }
        else
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, state.AcceptedForms);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(5, json.RootElement.GetProperty("fileCount").GetInt32());
        }
    }

    private static MultipartFormDataContent CreateFormWithTotalSize(int totalBytes)
    {
        // Пять файлов позволяют проверить общий лимит, не превышая 5 МиБ для отдельного файла.
        // Сначала считаем служебные байты multipart на пустых частях, затем выделяем остаток под содержимое.
        using var emptyForm = CreateMultipart(new int[5]);
        int overhead = checked((int)emptyForm.Headers.ContentLength!.Value);
        int[] fileSizes = [FileLimitBytes, FileLimitBytes, FileLimitBytes, FileLimitBytes, totalBytes - overhead - 4 * FileLimitBytes];
        var form = CreateMultipart(fileSizes);
        Assert.Equal(totalBytes, form.Headers.ContentLength);
        return form;
    }

    private static MultipartFormDataContent CreateMultipart(int[] sizes)
    {
        var form = new MultipartFormDataContent("amzn-size-test-boundary");
        for (int index = 0; index < sizes.Length; index++)
        {
            // Проверяется только передача формы и её размер, а не декодирование изображения.
            var file = new ByteArrayContent(new byte[sizes[index]]);
            file.Headers.ContentType = new("image/jpeg");
            form.Add(file, "files", $"photo-{index}.jpg");
        }
        return form;
    }
}
