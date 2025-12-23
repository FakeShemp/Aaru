// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Text User Interface.
//
// --[ License ] --------------------------------------------------------------
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as
//     published by the Free Software Foundation, either version 3 of the
//     License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2026 Natalia Portillo
// ****************************************************************************/

using CommunityToolkit.Mvvm.ComponentModel;

namespace Aaru.Tui.ViewModels;

public class ViewModelBase : ObservableObject, IRegionAware
{
    /// <summary>
    ///     Called to determine if this instance can handle the navigation request.
    ///     Don't call this directly, use <seealso cref="OnNavigatingTo(NavigationContext)" />.
    /// </summary>
    /// <param name="navigationContext">The navigation context.</param>
    /// <returns><see langword="true" /> if this instance accepts the navigation request; otherwise, <see langword="false" />.</returns>
    public virtual bool IsNavigationTarget(NavigationContext navigationContext) =>

        // Auto-allow navigation
        OnNavigatingTo(navigationContext);

    /// <summary>Called when the implementer is being navigated away from.</summary>
    /// <param name="navigationContext">The navigation context.</param>
    public virtual void OnNavigatedFrom(NavigationContext navigationContext) {}

    /// <summary>Called when the implementer has been navigated to.</summary>
    /// <param name="navigationContext">The navigation context.</param>
    public virtual void OnNavigatedTo(NavigationContext navigationContext) {}

    /// <summary>Navigation validation checker.</summary>
    /// <remarks>Override for Prism 7.2's IsNavigationTarget.</remarks>
    /// <param name="navigationContext">The navigation context.</param>
    /// <returns><see langword="true" /> if this instance accepts the navigation request; otherwise, <see langword="false" />.</returns>
    public virtual bool OnNavigatingTo(NavigationContext navigationContext) => true;
}