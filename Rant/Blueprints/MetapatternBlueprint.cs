using System;

using Rant.Compiler;

namespace Rant.Blueprints
{
    internal sealed class MetapatternBlueprint : Blueprint
    {
        public MetapatternBlueprint(Interpreter interpreter)
            : base(interpreter)
        {
        }

        public override bool Use()
        {
            var srcstr = I.PopResultString();
            var src = new RantPattern("Meta_" + String.Format("{0:X16}", srcstr.Hash()), SourceType.Metapattern, srcstr);
            I.PushState(new Interpreter.State(I, src, I.CurrentState.Output));
            return true;
        }
    }
}