using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public partial class InputManager : BaseManager
{
    public GameInputActions inputActions;

    public Dictionary<InputActionUIEnum, InputAction> dicInputUI = new Dictionary<InputActionUIEnum, InputAction>();
    public Dictionary<string, InputAction> dicInputPlayer = new Dictionary<string, InputAction>();

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
            dicInputPlayer.Add(itemData.name, itemData);
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
    /// <param name="name"></param>
    /// <returns></returns>
    public InputAction GetInputPlayerData(string name)
    {
        if (dicInputPlayer.TryGetValue(name, out InputAction value))
        {
            return value;
        }
        return null;
    }
}