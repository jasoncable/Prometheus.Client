﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Prometheus.Client.Collectors.Abstractions;
using Prometheus.Client.Contracts;

namespace Prometheus.Client.Collectors
{
    public class CollectorRegistry : ICollectorRegistry
    {
        public static readonly CollectorRegistry Instance = new CollectorRegistry();
        private readonly ConcurrentDictionary<string, ICollector> _collectors = new ConcurrentDictionary<string, ICollector>();
        private List<IOnDemandCollector> _onDemandCollectors;

        public void RegisterOnDemandCollectors(List<IOnDemandCollector> onDemandCollectors)
        {
            _onDemandCollectors = onDemandCollectors;

            foreach (var onDemandCollector in _onDemandCollectors)
                onDemandCollector.RegisterMetrics();
        }

        public IEnumerable<CMetricFamily> CollectAll()
        {
            if (_onDemandCollectors != null)
                foreach (var onDemandCollector in _onDemandCollectors)
                    onDemandCollector.UpdateMetrics();

            foreach (var value in _collectors.Values)
            {
                var c = value.Collect();
                if (c != null)
                    yield return c;
            }
        }

        public void Clear()
        {
            _collectors.Clear();
        }

        public ICollector GetOrAdd(ICollector collector)
        {
            var collectorToUse = _collectors.GetOrAdd(collector.Name, collector);

            if (!collector.LabelNames.SequenceEqual(collectorToUse.LabelNames))
                throw new ArgumentException("Collector with same name must have same label names");

            return collectorToUse;
        }

        public bool Remove(ICollector collector)
        {
            return _collectors.TryRemove(collector.Name, out _);
        }
    }
}