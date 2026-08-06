using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public partial class InputManager : BaseManager
{
    public GameInputActions inputActions;

    public Dictionary<InputActionUIEnum, InputAction> dicInputUI = new Dictionary<InputActionUIEnum, InputAction>();
    public Dictionary<InputActionPlayerEnum, InputAction> dicInputPlayer = new Dictionary<InputActionPlayerEnum, InputAction>();

    public virtual void Awake()
    {
        inputActions = new GameInputActions();
        //----------------------------------------------------------
        GameInputActions.UIActions uiActions = inputActions.UI;
        InputActionMap inputActionMapUI = uiActions.Get();
        ReadOnlyArray<InputAction> listUIData = inputActionMapUI.actions;

        foreach (var itemData in listUIData)
        {
            itemData.Enable();
            dicInputUI.Add(itemData.name.GetEnum<InputActionUIEnum>(), itemData);
        }

        //----------------------------------------------------------
        GameInputActions.PlayerActions playerActions = inputActions.Player;
        InputActionMap inputActionMapPlayer = playerActions.Get();
        ReadOnlyArray<InputAction> listPlayerData = inputActionMapPlayer.actions;

        foreach (var itemData in listPlayerData)
        {
            itemData.Enable();
            dicInputPlayer.Add(itemData.name.GetEnum<InputActionPlayerEnum>(), itemData);
        }
    }

    /// <summary>
    /// 获取UI数据
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public InputAction GetInputUIData(InputActionUIEnum name)
    {
        if (dicInputUI.TryGetValue(name, out InputAction value))
        {
            return value;
        }
        return null;
    }

    /// <summary>
    /// 获取Player数据
    /// </summary>
    /// <param name="name">Player 输入动作枚举</param>
    /// <returns></returns>
    public InputAction GetInputPlayerData(InputActionPlayerEnum name)
    {
        if (dicInputPlayer.TryGetValue(name, out InputAction value))
        {
            return value;
        }
        return null;
    }
}