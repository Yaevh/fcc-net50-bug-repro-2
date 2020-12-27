using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Szlem.Recruitment.Campaigns;
using Szlem.Recruitment.Impl;
using Xunit;
using Index = Szlem.Recruitment.Campaigns.Index;

namespace Szlem.Recruitment.Tests.Campaigns
{
    public class IndexTests
    {
        [Fact]
        public async Task EmptyDatabase_ReturnsNoCampaigns()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider();
            var mediator = sp.GetRequiredService<IMediator>();
            var query = new Index.Query() { EditionID = 1 };

            var result = await mediator.Send(query);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value);
        }
    }
}
