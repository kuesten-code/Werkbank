using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using MudBlazor;
using Kuestencode.Core.Enums;
using Kuestencode.Core.Models;
using Kuestencode.Faktura.Models;
using Kuestencode.Faktura.Models.Dashboard;
using Kuestencode.Faktura.Services;
using Kuestencode.Faktura.Shared;
using Kuestencode.Faktura.Shared.Components;

namespace Kuestencode.Faktura.Pages;

public partial class Dashboard
{
    private List<ServiceHealthItem> _healthItems = new();
    private DashboardSummary _summary = new();
    private List<ActivityItem> _activities = new();

    private bool _loadingHealth = true;
    private bool _loadingSummary = true;
    private bool _loadingActivities = true;

    private bool _healthError = false;
    private bool _summaryError = false;
    private bool _activitiesError = false;

    private System.Globalization.CultureInfo _culture = new System.Globalization.CultureInfo("de-DE");
    private Guid _refreshKey = Guid.NewGuid();

    protected override void OnInitialized()
    {
        NavigationManager.LocationChanged += HandleLocationChanged;
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadDashboardDataAsync();
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        // Nur neu laden wenn wir auf die Startseite navigieren
        var uri = new Uri(e.Location);
        if (uri.AbsolutePath == "/" || uri.AbsolutePath == "")
        {
            _refreshKey = Guid.NewGuid(); // Force re-render
            InvokeAsync(async () =>
            {
                await LoadDashboardDataAsync();
                StateHasChanged();
            });
        }
    }

    public void Dispose()
    {
        NavigationManager.LocationChanged -= HandleLocationChanged;
    }

    private async Task LoadDashboardDataAsync()
    {
        // Reset loading states
        _loadingHealth = true;
        _loadingSummary = true;
        _loadingActivities = true;
        _healthError = false;
        _summaryError = false;
        _activitiesError = false;

        // Load data sequentially to avoid DbContext threading issues
        await LoadHealthAsync();
        await LoadSummaryAsync();
        await LoadActivitiesAsync();
    }


    private async Task LoadHealthAsync()
    {
        try
        {
            var health = await DashboardService.GetHealthAsync();
            _healthItems = health.ToList();
        }
        catch
        {
            _healthError = true;
        }
        finally
        {
            _loadingHealth = false;
        }
    }

    private async Task LoadSummaryAsync()
    {
        try
        {
            _summary = await DashboardService.GetSummaryAsync();
        }
        catch
        {
            _summaryError = true;
        }
        finally
        {
            _loadingSummary = false;
        }
    }

    private async Task LoadActivitiesAsync()
    {
        try
        {
            var activities = await DashboardService.GetRecentActivitiesAsync(5);
            _activities = activities.ToList();
        }
        catch
        {
            _activitiesError = true;
        }
        finally
        {
            _loadingActivities = false;
        }
    }

    private string GetRelativeTime(DateTime date)
    {
        var timeSpan = DateTime.Now - date;

        if (timeSpan.TotalMinutes < 1)
            return "Gerade eben";
        if (timeSpan.TotalMinutes < 60)
            return $"vor {(int)timeSpan.TotalMinutes} Min.";
        if (timeSpan.TotalHours < 24)
            return $"vor {(int)timeSpan.TotalHours} Std.";
        if (timeSpan.TotalDays < 7)
            return $"vor {(int)timeSpan.TotalDays} Tag{((int)timeSpan.TotalDays > 1 ? "en" : "")}";

        return date.ToString("dd.MM.yyyy");
    }
}
