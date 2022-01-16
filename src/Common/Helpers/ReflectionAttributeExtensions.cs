using Prometheus;
using System;
using System.ComponentModel;
using PromMetrics = Prometheus.Metrics;
using Metrics = Common.Observe.Metrics;
using Common.Observe;

namespace Common.Helpers
{
    public static class ReflectionAttributeExtensions
    {
        private const string GetFieldDescriptionName = nameof(GetFieldDescription);

        private static readonly Histogram ReflectionActionDuration = PromMetrics.CreateHistogram("p2g_reflection_duration_seconds", "Histogram of reflection actions.", new HistogramConfiguration()
        {
            LabelNames = new[] { Metrics.Label.ReflectionMethod }
        });

        public static string GetFieldDescription(this System.Type type, string fieldName)
        {
            using var metrics = ReflectionActionDuration
                                    .WithLabels(GetFieldDescriptionName)
                                    .NewTimer();
            using var tracing = Tracing.Trace(GetFieldDescriptionName);

            var properties = TypeDescriptor.GetProperties(type);
            if (properties is null || properties.Count == 0) return string.Empty;

            try
            {
                var attributes = properties[fieldName].Attributes;
                
                if (attributes is null || attributes.Count <= 0) return string.Empty;

                var description = (DescriptionAttribute)attributes[typeof(DescriptionAttribute)];
                return description?.Description ?? string.Empty;

            } catch (Exception) { return string.Empty; }
        }
    }
}
