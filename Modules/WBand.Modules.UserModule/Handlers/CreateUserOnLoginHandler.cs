using System;
using System.Collections.Generic;
using System.Text;
using BandService.Contracts;
using Microsoft.EntityFrameworkCore;
using Serilog;
using WBand.Modules.UserModule.Data;
using WBand.Modules.UserModule.Domain;
using WBand.Modules.UserModule.Events;
using Wolverine;

namespace WBand.Modules.UserModule.Handlers;

public static class CreateUserOnLoginHandler
{
    public static async Task Handle(
        UserLoggedIn @event,
        IDataContext dataContext,
        IMessageBus messageBus,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var keycloakUserId = new KeycloakUserId(@event.UserId);
            var existingUser = await dataContext.Users.FirstOrDefaultAsync(
                x => x.KeycloakId == keycloakUserId,
                cancellationToken
            );
            if (existingUser is not null)
            {
                return;
            }

            var user = User.Create(keycloakUserId, Email.Create(@event.UserName));
            await dataContext.Users.AddAsync(user, cancellationToken);
            await dataContext.SaveChangesAsync(cancellationToken);

            await messageBus.PublishAsync(new UserCreated(user.Id.Value, user.Email.Value));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating user on login");
        }
    }
}
