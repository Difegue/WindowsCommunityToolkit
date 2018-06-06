// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Graph;
using Microsoft.Toolkit.Services.MicrosoftGraph;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.Toolkit.Uwp.UI.Controls.Graph
{
    /// <summary>
    /// Defines the events for the <see cref="PeoplePicker"/> control.
    /// </summary>
    public partial class PeoplePicker : Control
    {
        private static void AllowMultiplePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as PeoplePicker;
            if (!control.AllowMultiple)
            {
                if (control.Selections != null)
                {
                    control.Selections.Clear();
                    control.RaiseSelectionChanged();
                }

                if (control._searchBox != null)
                {
                    control._searchBox.Text = string.Empty;
                }
            }
        }

        private void ClearAndHideSearchResultListBox()
        {
            SearchResults.Clear();
            _searchResultPopup.IsOpen = false;
        }

        private async void SearchBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var textboxSender = (TextBox)sender;
            string searchText = textboxSender.Text.Trim();
            if (string.IsNullOrWhiteSpace(searchText))
            {
                ClearAndHideSearchResultListBox();
                return;
            }

            IsLoading = true;
            try
            {
                var graphService = MicrosoftGraphService.Instance;
                await graphService.TryLoginAsync();
                GraphServiceClient graphClient = graphService.GraphProvider;

                if (graphClient != null)
                {
                    var options = new List<QueryOption>
                    {
                        new QueryOption("$search", $"\"{searchText}\""),
                        new QueryOption("$filter", "personType/class eq 'Person' and personType/subclass eq 'OrganizationUser'")
                    };
                    IUserPeopleCollectionPage rawResults = await graphClient.Me.People.Request(options).GetAsync();

                    if (rawResults.Any())
                    {
                        SearchResults.Clear();

                        var results = rawResults.Where(o => !Selections.Any(s => s.Id == o.Id))
                            .Take(SearchResultLimit > 0 ? SearchResultLimit : DefaultSearchResultLimit);
                        foreach (var item in results)
                        {
                            SearchResults.Add(item);
                        }

                        _searchResultPopup.IsOpen = true;
                    }
                    else
                    {
                        ClearAndHideSearchResultListBox();
                    }
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void SearchResultListBox_OnSelectionChanged(object sender, Windows.UI.Xaml.Controls.SelectionChangedEventArgs e)
        {
#pragma warning disable SA1119 // Statement must not use unnecessary parenthesis
            if (!((sender as ListBox)?.SelectedItem is Person person))
#pragma warning restore SA1119 // Statement must not use unnecessary parenthesis
            {
                return;
            }

            if (!AllowMultiple && Selections.Any())
            {
                Selections.Clear();
                Selections.Add(person);
            }
            else
            {
                Selections.Add(person);
            }

            RaiseSelectionChanged();

            _searchBox.Text = string.Empty;
        }

        private void SelectionsListBox_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            var elem = e.OriginalSource as FrameworkElement;

            var removeButton = elem.FindAscendantByName("PersonRemoveButton");
            if (removeButton != null)
            {
                if (removeButton.Tag is Person item)
                {
                    Selections.Remove(item);
                    RaiseSelectionChanged();
                }
            }
        }

        private void RaiseSelectionChanged()
        {
            SelectionChanged?.Invoke(this, new PeopleSelectionChangedEventArgs(this.Selections));
        }

        private void SearchBox_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            _searchResultListBox.Width = _searchBox.ActualWidth;
        }
    }
}