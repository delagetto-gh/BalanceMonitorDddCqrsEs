﻿using BalanceMonitor.Accounting.Application.Projections;
using BalanceMonitor.Infrastructure.Core.Interfaces.Cqrs;
using System.ServiceModel;

namespace BalanceMonitor.Accounting.Application
{
  [ServiceContract]
  public interface IAccountingService : ICommandBus,
                                        IAccountDailyBalanceQuerier,
                                        IAccountAuditQuerier
  {
  }
}
