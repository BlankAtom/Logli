// Copyright (C) 2017 Alaa Masoud
// See the LICENSE file in the project root for more information.

using System;

namespace sample.Models
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; }

        public bool ShowRequestId => !String.IsNullOrEmpty(RequestId);
    }
}