using System;
using System.Linq;
using System.Reactive.Linq;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Execution;
using HotChocolate.Subscriptions;
using Microsoft.EntityFrameworkCore;
using monakS.BackgroundServices;
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
}