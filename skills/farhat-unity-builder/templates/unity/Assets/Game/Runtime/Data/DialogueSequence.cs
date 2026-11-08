using System;
using System.Collections.Generic;
using UnityEngine;

namespace Farhat.Data
{
    [Serializable]
    public class DialogueLine
    {
        public string speakerId;
        public string locKey;
        [TextArea(1, 4)] public string fallbackText;
        public DialogueTone tone;
        public float seconds = 3f;
    }

    [CreateAssetMenu(menuName = "Farhat/Dialogue Sequence")]
    public class DialogueSequence : ScriptableObject
    {
        public string id;
        public List<DialogueLine> lines = new List<DialogueLine>();
    }
}
