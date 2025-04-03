﻿using Corely.DataAccess.Interfaces.UnitOfWork;

namespace Corely.DataAccess.Mock;

public class MockUoWProvider : IUnitOfWorkProvider
{
    public Task BeginAsync()
    {
        return Task.CompletedTask;
    }

    public Task CommitAsync()
    {
        return Task.CompletedTask;
    }

    public Task RollbackAsync()
    {
        return Task.CompletedTask;
    }
}
