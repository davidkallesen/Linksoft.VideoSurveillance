global using System;
global using System.Collections.Generic;
global using System.Collections.ObjectModel;
global using System.Diagnostics.CodeAnalysis;
global using System.Globalization;
global using System.Linq;
global using System.Net.Http;
global using System.Text.Json;
global using System.Threading;
global using System.Threading.Tasks;
global using System.Windows;
global using System.Windows.Controls;
global using System.Windows.Data;
global using System.Windows.Input;
global using System.Windows.Interop;
global using System.Windows.Media;
global using System.Windows.Threading;

global using Atc.Rest.Api.SourceGenerator;
global using Atc.XamlToolkit.Controls.Attributes;
global using Atc.XamlToolkit.Mvvm;

global using ControlzEx.Theming;

global using Linksoft.VideoEngine;

global using Linksoft.VideoSurveillance.Services;

global using Linksoft.VideoSurveillance.Wpf.Dialogs;
global using Linksoft.VideoSurveillance.Wpf.Events;
global using Linksoft.VideoSurveillance.Wpf.Services;
global using Linksoft.VideoSurveillance.Wpf.UserControls;
global using Linksoft.VideoSurveillance.Wpf.ViewModels;
global using Linksoft.VideoSurveillance.Wpf.Windows;

global using Microsoft.AspNetCore.SignalR.Client;

global using VideoSurveillance.Generated.Cameras.Client;
global using VideoSurveillance.Generated.Cameras.Endpoints.Interfaces;
global using VideoSurveillance.Generated.Cameras.Models;
global using VideoSurveillance.Generated.Layouts.Client;
global using VideoSurveillance.Generated.Layouts.Endpoints.Interfaces;
global using VideoSurveillance.Generated.Layouts.Models;
global using VideoSurveillance.Generated.Recordings.Client;
global using VideoSurveillance.Generated.Recordings.Endpoints.Interfaces;
global using VideoSurveillance.Generated.Recordings.Models;
global using VideoSurveillance.Generated.Settings.Client;
global using VideoSurveillance.Generated.Settings.Endpoints.Interfaces;
global using VideoSurveillance.Generated.Settings.Models;