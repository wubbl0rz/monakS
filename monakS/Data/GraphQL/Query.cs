using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Execution;
using HotChocolate.Subscriptions;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using monakS.BackgroundServices;
using monakS.Hubs;
using monakS.Models;

namespace monakS.Data.GraphQL
{
  public class Query
  {
    [UseFiltering]
    [UseSorting]
    public IQueryable<Camera> Cameras([Service] AppDbContext context) =>
      context.Cameras;

    [UseFiltering]
    [UseSorting]
    public IQueryable<CaptureInfo> Captures([Service] AppDbContext context) =>
      context.CaptureInfos.Include(c => c.Cam);
  }

  public class Subscription
  {
    private readonly MessageEventBus _eventBus;

    public Subscription(MessageEventBus eventBus)
    {
      _eventBus = eventBus;
    }

    [UseFiltering]
    [SubscribeAndResolve]
    public async IAsyncEnumerable<CaptureInfo[]>
      OnActiveCapturesChanged([Service] AppDbContext ctx,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
      CaptureInfo[] MakeDbQuery()
      {
        return ctx.CaptureInfos.AsNoTracking().Include(c => c.Cam).ToArray();
      }

      yield return MakeDbQuery();
      
      await foreach (var msg in _eventBus.ToAsyncEnumerable<CaptureStatusMessage>(cancellationToken))
      {
        yield return MakeDbQuery();
      }
    }
  }
}