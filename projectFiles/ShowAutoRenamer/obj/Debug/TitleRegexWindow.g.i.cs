﻿#pragma checksum "..\..\TitleRegexWindow.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "152BC317D3E5BA0D29256AF7C304087CA6F7C213"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using ShowAutoRenamer;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace ShowAutoRenamer {
    
    
    /// <summary>
    /// TitleRegexWindow
    /// </summary>
    public partial class TitleRegexWindow : System.Windows.Window, System.Windows.Markup.IComponentConnector {
        
        
        #line 15 "..\..\TitleRegexWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button buttonDone;
        
        #line default
        #line hidden
        
        
        #line 16 "..\..\TitleRegexWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox textBoxTitleRegex;
        
        #line default
        #line hidden
        
        
        #line 17 "..\..\TitleRegexWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock textBlockTitlePreview;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/ShowAutoRenamer;component/titleregexwindow.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\TitleRegexWindow.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            this.buttonDone = ((System.Windows.Controls.Button)(target));
            
            #line 15 "..\..\TitleRegexWindow.xaml"
            this.buttonDone.Click += new System.Windows.RoutedEventHandler(this.DoneButtonClick);
            
            #line default
            #line hidden
            return;
            case 2:
            this.textBoxTitleRegex = ((System.Windows.Controls.TextBox)(target));
            
            #line 16 "..\..\TitleRegexWindow.xaml"
            this.textBoxTitleRegex.TextChanged += new System.Windows.Controls.TextChangedEventHandler(this.textBoxTitleRegex_TextChanged);
            
            #line default
            #line hidden
            return;
            case 3:
            this.textBlockTitlePreview = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 4:
            
            #line 19 "..\..\TitleRegexWindow.xaml"
            ((System.Windows.Shapes.Rectangle)(target)).MouseDown += new System.Windows.Input.MouseButtonEventHandler(this.Rectangle_MouseDown);
            
            #line default
            #line hidden
            return;
            case 5:
            
            #line 25 "..\..\TitleRegexWindow.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.Close_Click);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}

