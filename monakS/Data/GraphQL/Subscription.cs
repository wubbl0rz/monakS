using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using monakS.BackgroundServices;
using monakS.Hubs;
using monakS.Models;

namespace monakS.Data.GraphQL
{
  public class Subscription
  {
    private readonly MessageEventBus _eventBus;

    public Subscription(MessageEventBus eventBus)
    {
      _eventBus = eventBus;
    }
    
    [UseFiltering]
    [SubscribeAndResolve]
    public async IAsyncEnumerable<Camera[]>
      OnCamerasChanged([Service] AppDbContext ctx,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
      Camera[] MakeDbQuery()
      {
        return ctx.Cameras.AsNoTracking().ToArray();
      }
      
      yield return MakeDbQuery();

      await foreach (var msg in _eventBus.ToAsyncEnumerable<CameraUpdatedMessage>(cancellationToken))
      {
        yield return MakeDbQuery();
      }
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