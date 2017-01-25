﻿using BalanceMonitor.Accounting.Domain.Model;
using BalanceMonitor.Infrastructure.Core;
using BalanceMonitor.Infrastructure.Core.Interfaces.EventSourcing;
using BalanceMonitor.Infrastructure.Core.Interfaces.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BalanceMonitor.Accounting.Domain.Services
{
  public class AccountRepository : IAccountRepository
  {
    private readonly ISessionFactory session;

    public AccountRepository(ISessionFactory session)
    {
      this.session = session;
    }

    public Account Get(Guid id)
    {
      Account agg = null;
      using (var ctx = this.session.Open<BalanceMonitorAccountingSession>())
      {
        IEnumerable<VersionedDomainEvent> events = ctx.Events.Where(evt => evt.AggregateId == id);
        if (events.Any())
        {
          agg = new Account();
          agg.LoadFromHistory(events);
        }
        ctx.Commit();
      }
      return agg;
    }

    public void Add(Account aggregate)
    {
      using (var ctx = this.session.Open<BalanceMonitorAccountingSession>())
      {
        ///Don't save the entity if it hasnt had any changes applied to it.
        if (aggregate.UncommitedChanges.Any())
        {
          if (aggregate.Version != -1) //its not a new aggregate
          {
            var eventStoredAggregate = this.Get(aggregate.Id);
            if (aggregate.Version != eventStoredAggregate.Version)
            {
              throw new Exception(String.Format("Optimistic Concurrency! Aggregate {0} version inconsistency. Aggregate has been modified", aggregate.GetType()));
            }
          }

          int currentVersion = aggregate.Version;
          foreach (var @event in aggregate.UncommitedChanges)
          {
            currentVersion++; //local increment based off entity's starting version
            @event.Version = currentVersion; //set the event version to it
            aggregate.Version = currentVersion;
            ctx.Events.Add(@event);
          }

          ctx.Commit(); //ctx.commit() behind the scenes will 1)store to eventstore 2)also publish events to handlers!)
        }
      }
    }
  }
}