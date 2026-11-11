using System.Xml;
using AMZN.Data.Entities;
using AMZN.DTOs.Account;
using AMZN.DTOs.Auth;
using AMZN.Repositories.Users;
using AMZN.Security.Passwords;
using AMZN.Services.Auth;
using AMZN.Shared.Exceptions;
using AMZN.Shared.Exceptions.Errors;
using AMZN.Shared.Limits;
using AMZN.Shared.Transactions;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;

namespace AMZN.Services.Account;

public class AccountService
{
    private readonly IUserRepository _userRepository;
    private readonly IDeletedUserRepository _deletedUserRepository;
    private readonly ITransactionManager _transactionManager;

    public AccountService(
        IUserRepository userRepository,
        IDeletedUserRepository deletedUserRepository,
        ITransactionManager transactionManager)
    {
        _userRepository = userRepository;
        _deletedUserRepository = deletedUserRepository;
        _transactionManager = transactionManager;
    }


    public async Task<ProfileUpdateResponseDto> UpdateProfileAsync(ProfileUpdateRequestDto dto, Guid userId)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null)
        {
            throw new ApiException(ErrorCodes.UserNotFound, "User not found", StatusCodes.Status404NotFound);
        }

        string email = dto.Email.Trim().ToLowerInvariant();
        if (email != user.Email)
        {
            if (await _userRepository.IsEmailTakenAsync(email))
            {
                throw new ApiException(ErrorCodes.AuthEmailTaken, "Email already taken",
                    StatusCodes.Status409Conflict);
            }

            user.Email = email;
        }

        if (!string.IsNullOrEmpty(dto.FirstName)) user.FirstName = dto.FirstName.Trim();
        if (!string.IsNullOrEmpty(dto.LastName)) user.LastName = dto.LastName.Trim();
        if (!string.IsNullOrEmpty(dto.PhoneNumber)) user.PhoneNumber = dto.PhoneNumber;
        if (dto.BirthDate != null)
        {
            if (CheckUserAge(dto.BirthDate) < UserLimits.ProfileMinAgeYears)
            {
                throw new ApiException(ErrorCodes.ValidationError, $"User must be at least {UserLimits.ProfileMinAgeYears} years old",
                    StatusCodes.Status400BadRequest);
            }

            user.BirthDate = dto.BirthDate;
        }

        await _userRepository.UpdateUserAsync(user);
        return new ProfileUpdateResponseDto()
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role.ToString(),
            PhoneNumber = user.PhoneNumber,
            BirthDate = user.BirthDate,
        };
    }


    public int CheckUserAge(DateOnly? birthDate)
    {
        if (birthDate != null)
        {
            int age = DateTime.Now.Year - birthDate.Value.Year;
            if (DateTime.Now.Month < birthDate.Value.Month ||
                (DateTime.Now.Month == birthDate.Value.Month && DateTime.Now.Day < birthDate.Value.Day))
            {
                age--;
            }

            return age;
        }

        return 0;
    }

    public async Task DeleteUserAsync(Guid userId)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null)
        {
            throw new ApiException(ErrorCodes.UserNotFound, "User not found", StatusCodes.Status404NotFound);
        }

        DeletedUser deletedUser = new()
        {
            Id = Guid.NewGuid(),
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            BirthDate = user.BirthDate,
            DeletedAt = DateTime.UtcNow,
        };
        await _userRepository.DeleteUserAsync(user);
        await _deletedUserRepository.AddAsync(deletedUser);
    }

    public async Task<PaymentMethodResponseDto> AddPaymentMethod(PaymentMethodRequestDto dto, Guid userId)
    {
        return await _transactionManager.ExecuteAsync(async () =>
        {
            var user = await _userRepository.GetByIdForUpdateAsync(userId);
            if (user == null)
            {
                throw new ApiException(ErrorCodes.UserNotFound, "User not found", StatusCodes.Status404NotFound);
            }

            user.PaymentMethods = await _userRepository.GetUserPaymentMethodsAsync(user);
            if (user.PaymentMethods.Any(pm => pm.CardNumber == dto.CardNumber))
            {
                throw new ApiException(ErrorCodes.ValidationError, "Card number already exists",
                    StatusCodes.Status409Conflict);
            }

            if (dto.ExpirationDate < DateOnly.FromDateTime(DateTime.Now))
            {
                throw new ApiException(ErrorCodes.ValidationError, "Expiration date must be in the future",
                    StatusCodes.Status406NotAcceptable);
            }

            PaymentMethod paymentMethod = new()
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CardNumber = dto.CardNumber,
                ExpirationDate = dto.ExpirationDate,
                IsDefault = dto.IsDefault,
                User = user
            };
            if (paymentMethod.IsDefault)
            {
                _userRepository.SetDefaultPaymentMethod(paymentMethod, user);
            }

            await _userRepository.AddPaymentMethodAsync(paymentMethod, user);
            return new PaymentMethodResponseDto()
            {
                Id = paymentMethod.Id,
                CardNumber = paymentMethod.CardNumber,
                HolderFirstName = user.FirstName,
                HolderLastName = user.LastName,
                ExpirationDate = paymentMethod.ExpirationDate,
                IsDefault = paymentMethod.IsDefault,
            };
        });
    }

    public async Task<List<PaymentMethodResponseDto>> GetUserPaymentMethods(Guid userId)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null)
        {
            throw new ApiException(ErrorCodes.UserNotFound, "User not found", StatusCodes.Status404NotFound);
        }

        var paymentMethods = await _userRepository.GetUserPaymentMethodsAsync(user);
        return paymentMethods.Select(pm => new PaymentMethodResponseDto()
        {
            Id = pm.Id,
            CardNumber = pm.CardNumber,
            HolderFirstName = user.FirstName,
            HolderLastName = user.LastName,
            ExpirationDate = pm.ExpirationDate,
            IsDefault = pm.IsDefault,
        }).ToList();
    }

    public async Task<DeliveryAddressResponseDto> AddDeliveryAddress(DeliveryAddressRequestDto dto, Guid userId)
    {
        return await _transactionManager.ExecuteAsync(async () =>
        {
            var user = await _userRepository.GetByIdForUpdateAsync(userId);
            if (user == null)
            {
                throw new ApiException(ErrorCodes.UserNotFound, "User not found", StatusCodes.Status404NotFound);
            }

            user.DeliveryAddresses = await _userRepository.GetUserDeliveryAddressesAsync(user);
            if (user.DeliveryAddresses.Any(da => da.Address == dto.StreetAddress))
            {
                throw new ApiException(ErrorCodes.ValidationError, "Address already exists",
                    StatusCodes.Status409Conflict);
            }

            DeliveryAddress deliveryAddress = new()
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Address = dto.StreetAddress,
                City = dto.City,
                PostalCode = dto.PostalCode,
                Country = dto.Country,
                State = dto.State,
                PhoneNumber = dto.PhoneNumber,
                IsDefault = dto.IsDefault,
                User = user
            };

            if (deliveryAddress.IsDefault)
            {
                _userRepository.SetDefaultDeliveryAddress(deliveryAddress, user);
            }

            await _userRepository.AddDeliveryAddressAsync(deliveryAddress, user);

            return new DeliveryAddressResponseDto()
            {
                Id = deliveryAddress.Id,
                FirstName = deliveryAddress.FirstName,
                LastName = deliveryAddress.LastName,
                Address = deliveryAddress.Address,
                City = deliveryAddress.City,
                PostalCode = deliveryAddress.PostalCode,
                Country = deliveryAddress.Country,
                State = deliveryAddress.State,
                PhoneNumber = deliveryAddress.PhoneNumber,
                IsDefault = deliveryAddress.IsDefault,
            };
        });
    }

    public async Task<List<DeliveryAddressResponseDto>> GetUserDeliveryAddresses(Guid userId)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null)
        {
            throw new ApiException(ErrorCodes.UserNotFound, "User not found", StatusCodes.Status404NotFound);
        }

        var deliveryAddresses = await _userRepository.GetUserDeliveryAddressesAsync(user);
        /*return deliveryAddresses.Select(da => new DeliveryAddressResponseDto()
        {
            Id = da.Id,
            FirstName = da.FirstName,
            LastName = da.LastName,
            Address = da.Address,
            City = da.City,
            PostalCode = da.PostalCode,
            Country = da.Country,
            State = da.State,
            PhoneNumber = da.PhoneNumber,
            IsDefault = da.IsDefault,
        }).ToList();*/
        var response = new List<DeliveryAddressResponseDto>();
        foreach (var da in deliveryAddresses)
        {
            var dto = new DeliveryAddressResponseDto()
            {
                Id = da.Id,
                FirstName = da.FirstName,
                LastName = da.LastName,
                Address = da.Address,
                City = da.City,
                PostalCode = da.PostalCode,
                Country = da.Country,
                State = da.State,
                PhoneNumber = da.PhoneNumber,
                IsDefault = da.IsDefault,
            };
            response.Add(dto);
        }
        return response;
    }
}
