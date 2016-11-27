﻿using AspNetCore.Identity.DocumentDb.Stores;
using AspNetCore.Identity.DocumentDb.Tests.Builder;
using AspNetCore.Identity.DocumentDb.Tests.Comparer;
using AspNetCore.Identity.DocumentDb.Tests.Fixtures;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AspNetCore.Identity.DocumentDb.Tests
{
    [Collection("DocumentDbCollection")]
    public class RoleStoreTests : StoreTestsBase
    {
        public RoleStoreTests(DocumentDbFixture documentDbFixture)
            : base(documentDbFixture)
        {
            this.collectionUri = UriFactory.CreateDocumentCollectionUri(this.documentDbFixture.Database, this.documentDbFixture.RoleStoreDocumentCollection);
        }

        [Fact]
        public async Task ShouldCreateNewRoleInDatabase()
        {
            DocumentDbIdentityRole newRole = DocumentDbIdentityRoleBuilder.Create().WithId();
            DocumentDbRoleStore<DocumentDbIdentityRole> store = InitializeDocumentDbRoleStore();

            // Create the new role
            IdentityResult result = store.CreateAsync(newRole, CancellationToken.None).Result;

            // Get it again from the DB to check if it was created correctly
            DocumentDbIdentityRole queriedRole = await store.FindByIdAsync(newRole.Id, CancellationToken.None);

            Assert.True(result.Succeeded);
            Assert.Equal(queriedRole, newRole, new DocumentDbIdentityRoleComparer());
        }

        [Fact]
        public async Task ShouldUpdateExistingRoleInDatabase()
        {
            DocumentDbRoleStore<DocumentDbIdentityRole> store = InitializeDocumentDbRoleStore();
            DocumentDbIdentityRole existingRole = DocumentDbIdentityRoleBuilder.Create().WithId();

            // Create sample data in DB
            CreateDocument(existingRole);

            // Change property to upate on sample data and call the update mehtod
            existingRole.Name = Guid.NewGuid().ToString();
            IdentityResult result = await store.UpdateAsync(existingRole, CancellationToken.None);

            // Get it again from the DB to check if it was created correctly
            DocumentDbIdentityRole queriedRole = await store.FindByIdAsync(existingRole.Id, CancellationToken.None);

            Assert.True(result.Succeeded);
            Assert.Equal(existingRole, queriedRole, new DocumentDbIdentityRoleComparer());
        }

        [Fact]
        public async Task ShouldReturnRoleById()
        {
            DocumentDbRoleStore<DocumentDbIdentityRole> store = InitializeDocumentDbRoleStore();
            DocumentDbIdentityRole targetRole = DocumentDbIdentityRoleBuilder.Create().WithId();

            // Create sample data in DB
            CreateDocument(DocumentDbIdentityRoleBuilder.Create());
            CreateDocument(DocumentDbIdentityRoleBuilder.Create());
            CreateDocument(targetRole);
            CreateDocument(DocumentDbIdentityRoleBuilder.Create());

            DocumentDbIdentityRole queriedRole = await store.FindByIdAsync(targetRole.Id, CancellationToken.None);

            Assert.Equal(targetRole.Id, queriedRole.Id);
        }

        [Fact]
        public async Task ShouldDeleteRoleFromDb()
        {
            DocumentDbRoleStore<DocumentDbIdentityRole> store = InitializeDocumentDbRoleStore();
            DocumentDbIdentityRole targetRole = DocumentDbIdentityRoleBuilder.Create().WithId();

            // Create sample data in DB
            CreateDocument(DocumentDbIdentityRoleBuilder.Create());
            CreateDocument(DocumentDbIdentityRoleBuilder.Create());
            CreateDocument(targetRole);
            CreateDocument(DocumentDbIdentityRoleBuilder.Create());

            IdentityResult result = await store.DeleteAsync(targetRole, CancellationToken.None);

            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task ShouldReturnQueriedClaimFromRole()
        {
            string firstClaimType = Guid.NewGuid().ToString();
            string secondClaimType = Guid.NewGuid().ToString();
            string thirdClaimType = Guid.NewGuid().ToString();

            DocumentDbRoleStore<DocumentDbIdentityRole> store = InitializeDocumentDbRoleStore();
            DocumentDbIdentityRole targetRole = DocumentDbIdentityRoleBuilder.Create().WithId().AddClaim(firstClaimType).AddClaim(secondClaimType).AddClaim(thirdClaimType);

            IList<Claim> returnedClaims = await store.GetClaimsAsync(targetRole, CancellationToken.None);

            Assert.Collection(
                returnedClaims,
                c => c.Type.Equals(firstClaimType),
                c => c.Type.Equals(secondClaimType),
                c => c.Type.Equals(thirdClaimType));
        }

        [Fact]
        public async Task ShouldAddClaimToRole()
        {
            Claim newClaim = new Claim(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

            DocumentDbRoleStore <DocumentDbIdentityRole> store = InitializeDocumentDbRoleStore();
            DocumentDbIdentityRole targetRole = DocumentDbIdentityRoleBuilder.Create().WithId();

            await store.AddClaimAsync(targetRole, newClaim);

            Assert.Contains(targetRole.Claims, c => c.Type.Equals(newClaim.Type));
        }

        [Fact]
        public async Task ShouldRemoveClaimFromRole()
        {
            Claim claimToRemove = new Claim(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

            DocumentDbRoleStore<DocumentDbIdentityRole> store = InitializeDocumentDbRoleStore();
            DocumentDbIdentityRole targetRole = DocumentDbIdentityRoleBuilder.Create().WithId().AddClaim().AddClaim(claimToRemove).AddClaim();

            await store.RemoveClaimAsync(targetRole, claimToRemove, CancellationToken.None);

            Assert.DoesNotContain(targetRole.Claims, c => c.Type.Equals(claimToRemove.Type));
        }

        private DocumentDbRoleStore<DocumentDbIdentityRole> InitializeDocumentDbRoleStore()
        {
            return new DocumentDbRoleStore<DocumentDbIdentityRole>(
                documentClient: documentDbFixture.Client,
                options: Options.Create(new DocumentDbOptions()
                {
                    Database = documentDbFixture.Database,
                    UserStoreDocumentCollection = documentDbFixture.UserStoreDocumentCollection,
                    RoleStoreDocumentCollection = documentDbFixture.RoleStoreDocumentCollection
                }),
                normalizer: documentDbFixture.Normalizer);
        }
    }
}
