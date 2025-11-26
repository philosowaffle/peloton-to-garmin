using System;
using System.Collections.Generic;
using System.Xml;

namespace PelotonToGarmin.Sync.Merge.Utilities
{
    public static class TcxParser
    {
        public class Sample { public DateTime Time; public int? HeartRate; public double? Power; public int? Cadence; public double? Lat; public double? Lon; }

        public static List<Sample> ParseTcxToSeries(string tcxXml)
        {
            var outSamples = new List<Sample>();
            if (string.IsNullOrEmpty(tcxXml)) return outSamples;
            var doc = new XmlDocument();
            doc.LoadXml(tcxXml);
            var nsManager = new XmlNamespaceManager(doc.NameTable);
            nsManager.AddNamespace("t", "http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2");
            var nodes = doc.SelectNodes("//t:Trackpoint", nsManager);
            if (nodes == null) return outSamples;
            foreach (XmlNode node in nodes)
            {
                try
                {
                    var timeNode = node.SelectSingleNode("t:Time", nsManager);
                    var hrNode = node.SelectSingleNode(".//t:HeartRateBpm/t:Value", nsManager);
                    var latNode = node.SelectSingleNode("t:Position/t:LatitudeDegrees", nsManager);
                    var lonNode = node.SelectSingleNode("t:Position/t:LongitudeDegrees", nsManager);
                    var cadNode = node.SelectSingleNode("t:Cadence", nsManager);
                    var sample = new Sample();
                    if (timeNode != null) sample.Time = DateTime.Parse(timeNode.InnerText, null, System.Globalization.DateTimeStyles.AdjustToUniversal);
                    if (hrNode != null) sample.HeartRate = int.Parse(hrNode.InnerText);
                    if (cadNode != null) sample.Cadence = int.Parse(cadNode.InnerText);
                    if (latNode != null) sample.Lat = double.Parse(latNode.InnerText);
                    if (lonNode != null) sample.Lon = double.Parse(lonNode.InnerText);
                    outSamples.Add(sample);
                }
                catch { }
            }
            return outSamples;
        }
    }
}
