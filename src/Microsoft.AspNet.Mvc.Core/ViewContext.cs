// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System.IO;
using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc
{
    public class ViewContext : ActionContext
    {
        private DynamicViewData _viewBag;

        // We need a default FormContext if the user uses html <form> instead of an MvcForm
        private readonly FormContext _defaultFormContext = new FormContext();

        private FormContext _formContext;

        public ViewContext(
            [NotNull] ActionContext actionContext,
            [NotNull] IView view,
            [NotNull] ViewDataDictionary viewData,
            [NotNull] TextWriter writer)
            : base(actionContext)
        {
            View = view;
            ViewData = viewData;
            Writer = writer;

            _formContext = _defaultFormContext;
            UnobtrusiveJavaScriptEnabled = true;
            ClientValidationEnabled = true;
        }

        public ViewContext(
            [NotNull] ViewContext viewContext,
            [NotNull] IView view,
            [NotNull] ViewDataDictionary viewData,
            [NotNull] TextWriter writer)
            : base(viewContext)
        {
            _formContext = viewContext.FormContext;
            UnobtrusiveJavaScriptEnabled = viewContext.UnobtrusiveJavaScriptEnabled;
            ClientValidationEnabled = viewContext.ClientValidationEnabled;

            View = view;
            ViewData = viewData;
            Writer = writer;
        }

        public virtual FormContext FormContext
        {
            get
            {
                return _formContext;
            }
            set
            {
                // Never return a null form context, this is important for validation purposes.
                _formContext = value ?? _defaultFormContext;
            }
        }

        public bool UnobtrusiveJavaScriptEnabled { get; set; }

        public bool ClientValidationEnabled { get; set; }

        public dynamic ViewBag
        {
            get
            {
                if (_viewBag == null)
                {
                    _viewBag = new DynamicViewData(() => ViewData);
                }

                return _viewBag;
            }
        }

        public IView View { get; set; }

        public ViewDataDictionary ViewData { get; set; }

        public TextWriter Writer { get; set; }

        public FormContext GetFormContextForClientValidation()
        {
            return (ClientValidationEnabled) ? FormContext : null;
        }
    }
}