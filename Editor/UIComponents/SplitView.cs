using UnityEngine.UIElements;

namespace GameplayFramework
{
    public class SplitView : TwoPaneSplitView
    {
        public new class UxmlFactory : UxmlFactory<SplitView, TwoPaneSplitView.UxmlTraits> { }

        public SplitView()
        {

        }
    }
}

