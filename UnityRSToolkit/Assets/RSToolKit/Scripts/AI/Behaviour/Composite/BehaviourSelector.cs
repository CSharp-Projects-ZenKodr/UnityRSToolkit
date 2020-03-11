﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RSToolkit.AI.Behaviour.Composite
{
    public class BehaviourSelector : BehaviourSequenceSelectBase
    {
        public BehaviourSelector(bool isRandom) : base("Selector", isRandom)
        {
        }

        protected override void ProcessChildNodeSequence()
        {
            ProcessChildNodeSequence(false);
        }


        protected override void OnChildNodeStopped_Listener(BehaviourNode child, bool success)
        {
            if (success)
            {
                ProcessChildNodeSequence();
            }
            else
            {
                OnStopped.Invoke(false);
            }
        }

    }
}