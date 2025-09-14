using System.Collections.Generic;

namespace Cohere.Client.Models.V1;

/// <summary>
/// Request schema for Cohere API v1 Classify endpoint (/v1/classify).
/// Note: This endpoint is legacy; see docs for current guidance.
/// </summary>
public class ClassifyRequestV1
{
    public IList<string> Inputs { get; set; } = new List<string>();
    public IDictionary<string, IList<string>>? Examples { get; set; }
}

public class LabelPredictionV1
{
    public string Label { get; set; } = string.Empty;
    public double Confidence { get; set; }
}

public class ClassifyResponseV1
{
    public IList<LabelPredictionV1> Predictions { get; set; } = new List<LabelPredictionV1>();
}

