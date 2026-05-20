namespace AMZN.Shared.Exceptions.Errors
{
    // Коди Помилок
    public static class ErrorCodes
    {

        // DTO validation (ModelState)
        public const string ValidationError = "validation.error";

        // Auth
        public const string AuthEmailTaken = "auth.email_taken";
        public const string AuthInvalidCredentials = "auth.invalid_credentials";
        public const string AuthInvalidRefreshToken = "auth.invalid_refresh_token";
        public const string AuthClaimsInvalid = "auth.claims_invalid";

        // System
        public const string DatabaseError = "database.error";
        public const string InternalError = "internal.error";

        // Rate limiting
        public const string TooManyRequests = "rate_limited.too_many_requests";

        // Product
        public const string ProductNotFound = "product.not_found";
        public const string ProductOutOfStock = "product.out_of_stock";

        // Category
        public const string CategoryNotFound = "category.not_found";

        // Product Ratings
        public const string CannotRateOwnProduct = "product.cannot_rate_own";
        
        // User
        public const string UserNotFound = "user.not_found";

        // Product Review
        public const string CannotReviewOwnProduct = "product.cannot_review_own";

        // Product Questions
        public const string ProductQuestionNotFound = "product.question.not_found";
        
        // Cart
        public const string CartNotFound = "cart.not_found";
        
        // Order
        public const string OrderNotFound = "order.not_found";
        public const string PendingOrderExists = "order.pending_order_exists";

    }

}
