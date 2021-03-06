﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace WindowWalker.Components
{
    /// <summary>
    /// Responsible for searching and finding matches for the strings provided.
    /// Essentially the UI independent model of the application
    /// </summary>
    class WindowSearchController
    {
        #region Members

        /// <summary>
        /// the current search text
        /// </summary>
        private string searchText;

        /// <summary>
        /// Open window search results
        /// </summary
        private List<WindowSearchResult> searchMatches;

        /// <summary>
        /// Singleton pattern
        /// </summary>
        private static WindowSearchController instance;

        #endregion

        #region Events

        /// <summary>
        /// Delegate handler for open windows updates
        /// </summary>
        public delegate void SearchResultUpdateHandler(object sender, Window.WindowListUpdateEventArgs e);

        /// <summary>
        /// Event raised when there is an update to the list of open windows
        /// </summary>
        public event SearchResultUpdateHandler OnSearchResultUpdate;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the current search text
        /// </summary>
        public string SearchText
        {
            get { return searchText; }
            set 
            { 
                searchText = value.ToLower().Trim();
                this.SearchTextUpdated();
            }
        }

        /// <summary>
        /// Gets the open window search results
        /// </summary>
        public List<WindowSearchResult> SearchMatches
        {
            get 
            { return (new List<WindowSearchResult>(searchMatches)).OrderByDescending(x => x.Score).ToList(); }
        }

        /// <summary>
        /// Singleton Pattern
        /// </summary>
        public static WindowSearchController Instance
        {
            get
            {
                if (WindowSearchController.instance == null)
                {
                    WindowSearchController.instance = new WindowSearchController();
                }

                return WindowSearchController.instance;
            }

        }

        #endregion

        /// <summary>
        /// Initializes the search controller object
        /// </summary>
        private WindowSearchController()
        {
            this.searchText = string.Empty;
            OpenWindows.Instance.OnOpenWindowsUpdate += OpenWindowsUpdateHandler;
        }

        /// <summary>
        /// Event handler for when the search text has been updated
        /// </summary>
        public void SearchTextUpdated()
        {
            this.SyncOpenWindowsWithModelAsync();
        }

        #region Event Handlers
        /// <summary>
        /// Event handler called when the OpenWindows list changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OpenWindowsUpdateHandler(object sender, Window.WindowListUpdateEventArgs e)
        {
            this.SyncOpenWindowsWithModelAsync();
        }

        /// <summary>
        /// Syncs the open windows with the OpenWindows Model
        /// </summary>
        private async void SyncOpenWindowsWithModelAsync()
        {
            System.Diagnostics.Debug.Print("Syncing WindowSearch result with OpenWindows Model");

            List<Window> snapshotOfOpenWindows = OpenWindows.Instance.Windows;

            if (this.SearchText == string.Empty)
            {
                this.searchMatches = new List<WindowSearchResult>();
            }
            else
            {
                this.searchMatches = await FuzzySearchOpenWindowsAsync(snapshotOfOpenWindows);                    
            }

            if (this.OnSearchResultUpdate != null)
            {
                this.OnSearchResultUpdate(this, new Window.WindowListUpdateEventArgs());
            }
        }

        /// <summary>
        /// Redirecting method for Fuzzy searching
        /// </summary>
        /// <param name="openWindows"></param>
        /// <returns></returns>
        private Task<List<WindowSearchResult>> FuzzySearchOpenWindowsAsync(List<Window> openWindows)
        {
            return Task.Run(
                () =>
                    this.FuzzySearchOpenWindows(openWindows)
                );
        }

        /// <summary>
        /// Search method that matches the title of windows with the user search text
        /// </summary>
        /// <param name="openWindows"></param>
        /// <returns></returns>
        private List<WindowSearchResult> FuzzySearchOpenWindows(List<Window> openWindows)
        {
            List<WindowSearchResult> result = new List<WindowSearchResult>();
            List<SearchString> searchStrings = new List<SearchString>();

            List<string> shortcuts = SettingsManager.Instance.GetShortcut(this.SearchText);

            foreach(var shortcut in shortcuts)
            {
                searchStrings.Add(new SearchString(shortcut, WindowSearchResult.SearchType.Shortcut));
            }

            searchStrings.Add(new SearchString(this.searchText, WindowSearchResult.SearchType.Fuzzy));

            foreach (var searchString in searchStrings)
            {
                foreach (var window in openWindows)
                {
                    var titleMatch = FuzzyMatching.FindBestFuzzyMatch(window.Title, searchString.SearchText);
                    var processMatch = FuzzyMatching.FindBestFuzzyMatch(window.ProcessName, searchString.SearchText);

                    if ((titleMatch.Count != 0 || processMatch.Count != 0) &&
                                window.Title.Length != 0)
                    {
                        var temp = new WindowSearchResult(window, titleMatch, processMatch, searchString.SearchType);
                        result.Add(temp);
                    }
                }
            }

            System.Diagnostics.Debug.Print("Found " + result.Count + " windows that match the search text");

            return result;
        }

        /// <summary>
        /// A class to represent a search string
        /// </summary>
        /// <remarks>Class was added inorder to be able to attach various context data to
        /// a search string</remarks>
        class SearchString
        {
            /// <summary>
            /// Where is the search string coming from (is it a shortcut
            /// or direct string, etc...)
            /// </summary>
            public WindowSearchResult.SearchType SearchType
            {
                get;
                private set;
            }

            /// <summary>
            /// The actual text we are searching for
            /// </summary>
            public string SearchText
            {
                get;
                private set;
            }
            
            /// <summary>
            /// Constructor 
            /// </summary>
            /// <param name="searchText"></param>
            /// <param name="searchType"></param>
            public SearchString(string searchText, WindowSearchResult.SearchType searchType)
            {
                this.SearchText = searchText;
                this.SearchType = searchType;
            }
        }
        #endregion
    }
}
