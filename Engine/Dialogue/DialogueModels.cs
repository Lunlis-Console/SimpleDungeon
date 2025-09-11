using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SimpleDungeon.Engine.Dialogue
{
    // Верхний документ диалога
    public class DialogueDocument
    {
        [JsonProperty("schema", NullValueHandling = NullValueHandling.Ignore)]
        public string Schema { get; set; } = "dialogue.v1";

        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }

        [JsonProperty("start", NullValueHandling = NullValueHandling.Ignore)]
        public string Start { get; set; }

        [JsonProperty("nodes", NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<DialogueNode> Nodes { get; set; } = new();
    }

    public class DialogueNode
    {
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; }

        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
        public string Text { get; set; }

        [JsonProperty("responses", NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<Response> Responses { get; set; } = new();
    }

    public class Response
    {
        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
        public string Text { get; set; }

        // null or missing -> end dialogue
        [JsonProperty("target", NullValueHandling = NullValueHandling.Ignore)]
        public string Target { get; set; }

        [JsonProperty("actions", NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<DialogueAction> Actions { get; set; } = new();
    }

    public class DialogueAction
    {
        // e.g. "StartTrade", "GiveGold", "StartQuest", "GiveItem", "SetFlag"
        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }

        // string parameter; can be "50" for gold or "itemId:3" etc.
        [JsonProperty("param", NullValueHandling = NullValueHandling.Ignore)]
        public string Param { get; set; }
    }
}
