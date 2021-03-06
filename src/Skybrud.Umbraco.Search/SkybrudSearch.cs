﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Examine;
using Skybrud.Umbraco.Search.Models;
using Skybrud.Umbraco.Search.Options;
using Skybrud.Umbraco.Search.Options.Pagination;
using Skybrud.Umbraco.Search.Options.Sorting;
using Umbraco.Core.Logging;

namespace Skybrud.Umbraco.Search {

    public class SkybrudSearch {

        private readonly IExamineManager _examine;
        private readonly ILogger _logger;

        #region Constructors

        public SkybrudSearch(IExamineManager examine, ILogger logger) {
            _examine = examine;
            _logger = logger;
        }

        #endregion

        #region Member methods

        public SkybrudSearchResults Search(ISearchOptions options) {

            // Start measuring the elapsed time
            Stopwatch sw = Stopwatch.StartNew();

            // Get the searcher from the options
            ISearcher searcher = options.Searcher;

            // Fall back to the searcher of the external index if a searcher hasn't been specified
            if (options.Searcher == null) {

                if (_examine.TryGetIndex("ExternalIndex", out IIndex index) == false) throw new Exception("Examine index not found.");

                // Get the searcher from the index
                searcher = index.GetSearcher();
                if (searcher == null) throw new Exception("Examine searcher not found.");

            }

            // Get the raw query via the options
            string query = options.GetRawQuery();

            // Make the search in Examine
            ISearchResults allResults = searcher.CreateQuery().NativeQuery(query).Execute(int.MaxValue);

            long total = allResults.TotalItemCount;

            IEnumerable<ISearchResult> results = allResults;

            // If "options" implements the interface, results are sorted using the "Sort" method
            if (options is IPostSortOptions s) results = s.Sort(results);

            // If "options" implements implement the interface, the results are paginated
            if (options is IOffsetOptions o) results = results.Skip(o.Offset).Take(o.Limit);

            sw.Stop();

            if (options.IsDebug) {
                _logger.Debug<SkybrudSearch>("Search of type {Type} completed in {Milliseconds} with {Query}", options.GetType().FullName, sw.ElapsedMilliseconds, query);
            }

            // Wrap the results
            return new SkybrudSearchResults(options, query, total, results);

        }

        #endregion

    }

}