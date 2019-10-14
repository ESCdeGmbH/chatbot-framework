// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Microsoft.Bot.Builder.Dialogs;
using System.Collections.Generic;
using System.Reflection;

namespace Framework.Dialogs.StatefulWaterfallDialog
{
    /// <summary>
    /// Dialog optimized for prompting a user with a series of questions. Waterfalls accept a stack of
    /// functions which will be executed in sequence. Each waterfall step can ask a question of the user
    /// and the user's response will be passed as an argument to the next waterfall step.
    /// In contrast to the usual waterfall dialog this dialog provides a reset of steps and is empowered to handle its state in attributes.
    /// </summary>
    public abstract class StatefulWaterfallDialog : WaterfallDialog
    {

        private readonly List<WaterfallStep> _clearableSteps;

        /// <summary>
        /// Creates a WaterfallDialog.
        /// Do not add any steps here. 
        /// To add steps use <see cref="AddInitialSteps"/>.
        /// </summary>
        /// <param name="dialogId">The Id of the dialog.</param>
        public StatefulWaterfallDialog(string dialogId) : base(dialogId)
        {
            _clearableSteps = LoadStepAttribute();
            AddInitialSteps();
        }

        private List<WaterfallStep> LoadStepAttribute()
        {
            FieldInfo info = typeof(StatefulWaterfallDialog).BaseType.GetField("_steps", BindingFlags.Instance | BindingFlags.NonPublic);
            return (List<WaterfallStep>)info.GetValue(this);
        }

        /// <summary>
        /// Describes the initial configuration of the dialog.
        /// </summary>
        protected abstract void AddInitialSteps();

        /// <summary>
        /// Resets the state of the dialog.
        /// </summary>
        public virtual void Reset()
        {
            _clearableSteps.Clear();
            AddInitialSteps();
        }
    }
}
