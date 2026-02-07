using Migrator.Core;
using Migrator.Repositories;
using Migrator.Services;

namespace Migrator.Controllers
{
    sealed class OAuthControllers
    {
        public static async Task<IResult> Login(AuthRepository authRepository)
        {
            var redirectUri = authRepository.GetLoginRedirectUrl();

            return Results.Redirect(redirectUri);
        }

        public static async Task<IResult> Callback(string code, MigrateService migrateService)
        {
            await migrateService.StartMigration(code);

            return TypedResults.Ok("Callback");
        }
    }
}
