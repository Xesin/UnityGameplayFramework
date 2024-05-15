using UnityEngine;

namespace Xesin.GameplayFramework
{
    [CreateAssetMenu(menuName = "Gameplay/Input/Gamepad Images Collection", fileName = "GamepadImagesCollection.asset")]
    public class GamepadImageCollection : InputImageCollection
    {
        [SerializeField] private Sprite buttonSouth;
        [SerializeField] private Sprite buttonNorth;
        [SerializeField] private Sprite buttonEast;
        [SerializeField] private Sprite buttonWest;
        [SerializeField] private Sprite startButton;
        [SerializeField] private Sprite selectButton;
        [SerializeField] private Sprite leftTrigger;
        [SerializeField] private Sprite rightTrigger;
        [SerializeField] private Sprite leftShoulder;
        [SerializeField] private Sprite rightShoulder;
        [SerializeField] private Sprite dpad;
        [SerializeField] private Sprite dpadUp;
        [SerializeField] private Sprite dpadDown;
        [SerializeField] private Sprite dpadLeft;
        [SerializeField] private Sprite dpadRight;
        [SerializeField] private Sprite leftStick;
        [SerializeField] private Sprite rightStick;
        [SerializeField] private Sprite leftStickPress;
        [SerializeField] private Sprite rightStickPress;
        [SerializeField] private Sprite unrecognized;

        public override Sprite GetInputImage(string inputPath)
        {
            switch (inputPath)
            {
                case "buttonSouth": return buttonSouth;
                case "buttonNorth": return buttonNorth;
                case "buttonEast": return buttonEast;
                case "buttonWest": return buttonWest;
                case "start": return startButton;
                case "select": return selectButton;
                case "leftTrigger": return leftTrigger;
                case "rightTrigger": return rightTrigger;
                case "leftShoulder": return leftShoulder;
                case "rightShoulder": return rightShoulder;
                case "dpad": return dpad;
                case "dpad/up": return dpadUp;
                case "dpad/down": return dpadDown;
                case "dpad/left": return dpadLeft;
                case "dpad/right": return dpadRight;
                case "leftStick": return leftStick;
                case "rightStick": return rightStick;
                case "leftStickPress": return leftStickPress;
                case "rightStickPress": return rightStickPress;
            }
            return unrecognized;
        }
    }
}
