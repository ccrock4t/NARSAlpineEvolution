using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class WumpusUIController : MonoBehaviour
{
    // UI References (set these in Inspector)
    public Text CurrentRoomText;
    public Text PerceptionText;
    public Text GameStateText;
    public InputField RoomInput;
    public Button MoveButton;
    public Button ShootButton;

    private WumpusWorldGame game;

    void Start()
    {
        game = new WumpusWorldGame(); // from previous logic
        UpdateUI();

        MoveButton.onClick.AddListener(() => OnMoveClicked());
        ShootButton.onClick.AddListener(() => OnShootClicked());
    }

    void UpdateUI()
    {
        CurrentRoomText.text = $"Room: {game.PlayerRoom} → Connected to: {string.Join(", ", game.Rooms[game.PlayerRoom].Connections)}";
        PerceptionText.text = game.Perceive();

        if (!game.IsPlayerAlive)
            GameStateText.text = "💀 Game Over!";
        else if (!game.IsWumpusAlive)
            GameStateText.text = "🎉 You Win!";
        else
            GameStateText.text = $"Arrows left: {game.Arrows}";
    }

    void OnMoveClicked()
    {
        if (!int.TryParse(RoomInput.text, out int roomId)) return;

        if (!game.IsPlayerAlive || !game.IsWumpusAlive) return;

        var result = game.Move(roomId);
        Debug.Log(result);
        UpdateUI();
    }

    void OnShootClicked()
    {
        if (!int.TryParse(RoomInput.text, out int roomId)) return;

        if (!game.IsPlayerAlive || !game.IsWumpusAlive) return;

        var result = game.Shoot(roomId);
        Debug.Log(result);
        UpdateUI();
    }
}
