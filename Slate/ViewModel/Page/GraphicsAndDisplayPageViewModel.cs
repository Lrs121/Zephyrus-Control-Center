﻿using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Skia;
using Glitonea.Mvvm;
using Glitonea.Mvvm.Messaging;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Drawing;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Slate.Infrastructure;
using Slate.Infrastructure.Asus;
using Slate.Infrastructure.Services;
using Slate.Model.Messaging;
using Slate.Model.Settings.Components;

namespace Slate.ViewModel.Page
{
    public class GraphicsAndDisplayPageViewModel : ViewModelBase
    {
        private readonly ISettingsService _settingsService;
        private readonly IAsusHalService _asusHalService;

        protected IHardwareMonitorService HardwareMonitor { get; }
        
        public ISeries[] Series { get; set; } = new ISeries[]
        {
            new LineSeries<ObservablePoint>
            {
                Fill = null,
                LineSmoothness = 0.275,
                GeometryStroke = new SolidColorPaint(SKColors.DarkGray, 2),
                Stroke = new SolidColorPaint(SKColors.DarkGray, 2),
                GeometrySize = 8,
                DataPadding = new LvcPoint(5,5)
            }
        };

        public GraphicsAndDisplaySettings GraphicsAndDisplaySettings =>
            _settingsService.ControlCenter!.GraphicsAndDisplay;
        
        public GraphicsAndDisplayPageViewModel(
            ISettingsService settingsService,
            IAsusHalService asusHalService,
            IHardwareMonitorService hardwareMonitor)
        {
            _settingsService = settingsService;
            _asusHalService = asusHalService;
            HardwareMonitor = hardwareMonitor;

            var primaryColor = AvaloniaLocator
                .Current
                .GetRequiredService<IPlatformSettings>()
                .GetColorValues()
                .AccentColor1
                .ToSKColor();

            var series = (LineSeries<ObservablePoint>)Series[0];
            
            series.Values = GraphicsAndDisplaySettings.FanCurve!.ToChartValues();
            series.GeometryStroke = new SolidColorPaint(primaryColor, 2);
            series.Stroke = new SolidColorPaint(primaryColor, 2);
            
            Message.Subscribe<SystemAccentColorChangedMessage>(this, OnSystemAccentColorChanged);
        }
        
        public void HandleCurveModification()
        {
            var series = (LineSeries<ObservablePoint>)Series[0];
            var values = (ObservableCollection<ObservablePoint>)series.Values!;

            GraphicsAndDisplaySettings.FanCurve = values.ToFanCurve();
        }
        
        public void ActivatePreset(object? parameter)
        {
            var preset = (PerformancePreset)parameter!;
            var curve = _asusHalService.ReadBuiltInGpuFanCurve(preset);
            
            var series = (LineSeries<ObservablePoint>)Series[0];
            var oldValues = (ObservableCollection<ObservablePoint>)series.Values!;
            var newValues = curve.ToChartValues();
            GraphicsAndDisplaySettings.FanCurve = newValues.ToFanCurve();
            
            for (var i = 0; i < 8; i++)
                oldValues[i] = newValues[i];
        }

        private void OnSystemAccentColorChanged(SystemAccentColorChangedMessage msg)
        {
            var series = (LineSeries<ObservablePoint>)Series[0];
            
            series.GeometryStroke = new SolidColorPaint(msg.PrimaryAccentColor.ToSKColor(), 2);
            series.Stroke = new SolidColorPaint(msg.PrimaryAccentColor.ToSKColor(), 2);
        }
    }
}