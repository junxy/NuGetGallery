﻿using System;
using System.Data.Entity;
using System.Linq;
using OData.Linq;
using QueryInterceptor;

namespace NuGetGallery
{
    public static class PackageExtensions
    {
        private static readonly DateTime UnpublishedDate = new DateTime(1900, 1, 1, 0, 0, 0);

        public static IQueryable<V1FeedPackage> ToV1FeedPackageQuery(this IQueryable<Package> packages, string siteRoot)
        {
            siteRoot = EnsureTrailingSlash(siteRoot);
            return packages
                .Include(p => p.PackageRegistration)
                .WithoutNullPropagation()
                .Select(
                    p => new V1FeedPackage
                        {
                            Id = p.PackageRegistration.Id,
                            Version = p.Version,
                            Authors = p.FlattenedAuthors,
                            Copyright = p.Copyright,
                            Created = p.Created,
                            Dependencies = p.FlattenedDependencies,
                            Description = p.Description,
                            DownloadCount = p.PackageRegistration.DownloadCount,
                            ExternalPackageUrl = null,
                            GalleryDetailsUrl = siteRoot + "packages/" + p.PackageRegistration.Id + "/" + p.Version,
                            IconUrl = p.IconUrl,
                            IsLatestVersion = p.IsLatestStable,
                            Language = p.Language,
                            LastUpdated = p.LastUpdated,
                            LicenseUrl = p.LicenseUrl,
                            PackageHash = p.Hash,
                            PackageHashAlgorithm = p.HashAlgorithm,
                            PackageSize = p.PackageFileSize,
                            ProjectUrl = p.ProjectUrl,
                            Published = p.Listed ? p.Published : UnpublishedDate,
                            ReleaseNotes = p.ReleaseNotes,
                            ReportAbuseUrl = siteRoot + "package/ReportAbuse/" + p.PackageRegistration.Id + "/" + p.Version,
                            RequireLicenseAcceptance = p.RequiresLicenseAcceptance,
                            Summary = p.Summary,
                            Tags = p.Tags == null ? null : " " + p.Tags.Trim() + " ",
                            // In the current feed, tags are padded with a single leading and trailing space 
                            Title = p.Title ?? p.PackageRegistration.Id, // Need to do this since the older feed always showed a title.
                            VersionDownloadCount = p.DownloadCount,
                            Rating = 0
                        });
        }

        public static IQueryable<V2FeedPackage> ToV2FeedPackageQuery(this IQueryable<Package> packages, string siteRoot)
        {
            siteRoot = EnsureTrailingSlash(siteRoot);
            var packageQuery = packages
                .Include(p => p.PackageRegistration)
                .Include(p => p.LicenseReports)
                .WithoutNullPropagation();
            return from p in packageQuery
                   let licenseReport = p.LicenseReports.OrderByDescending(r => r.CreatedUtc)
                   select new V2FeedPackage
                    {
                        Id = p.PackageRegistration.Id,
                        Version = p.Version,
                        Authors = p.FlattenedAuthors,
                        Copyright = p.Copyright,
                        Created = p.Created,
                        Dependencies = p.FlattenedDependencies,
                        Description = p.Description,
                        DownloadCount = p.PackageRegistration.DownloadCount,
                        GalleryDetailsUrl = siteRoot + "packages/" + p.PackageRegistration.Id + "/" + p.Version,
                        IconUrl = p.IconUrl,
                        IsLatestVersion = p.IsLatestStable,
                        // To maintain parity with v1 behavior of the feed, IsLatestVersion would only be used for stable versions.
                        IsAbsoluteLatestVersion = p.IsLatest,
                        IsPrerelease = p.IsPrerelease,
                        LastUpdated = p.LastUpdated,
                        Language = p.Language,
                        PackageHash = p.Hash,
                        PackageHashAlgorithm = p.HashAlgorithm,
                        PackageSize = p.PackageFileSize,
                        ProjectUrl = p.ProjectUrl,
                        ReleaseNotes = p.ReleaseNotes,
                        ReportAbuseUrl = siteRoot + "package/ReportAbuse/" + p.PackageRegistration.Id + "/" + p.Version,
                        RequireLicenseAcceptance = p.RequiresLicenseAcceptance,
                        Published = p.Listed ? p.Published : UnpublishedDate,
                        Summary = p.Summary,
                        Tags = p.Tags,
                        Title = p.Title,
                        VersionDownloadCount = p.DownloadCount,
                        MinClientVersion = p.MinClientVersion,

                        // License Report Information
                        LicenseUrl = p.LicenseUrl
                    };
        }

        internal static IQueryable<TVal> WithoutVersionSort<TVal>(this IQueryable<TVal> feedQuery)
        {
            return feedQuery.InterceptWith(new ODataRemoveVersionSorter());
        }

        private static string EnsureTrailingSlash(string siteRoot)
        {
            if (!siteRoot.EndsWith("/", StringComparison.Ordinal))
            {
                siteRoot = siteRoot + '/';
            }
            return siteRoot;
        }
    }
}