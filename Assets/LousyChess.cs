using Assets;
using Engines;
using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

public class LousyChess : MonoBehaviour
{

    public KMBombInfo Bomb;
    public KMSelectable Module;
    public GameObject Squares;
    public GameObject FullPieces;
    public GameObject FlatPieces;
    public GameObject Selected;
    public KMSelectable Button;
    public GameObject DisplayText;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private KMSelectable[] _squares = new KMSelectable[30];
    private Game _game;
    private bool _solved = false;
    private GameObject _pieces;

    void Start()
    {
        _moduleId = _moduleIdCounter++;


        // This is for debugging:
        // _game = new Game { BombNumber = "0", WhiteEngine = new LetsSwitchSides(), BlackEngine = new LetsSwitchSides() };
        // _game.Board = new StringBuilder("..........5.....0.............");
        // _game.WhiteEngine.Init(Player.White, 0, _game.BombNumber);
        // _game.BlackEngine.Init(Player.Black, 0, _game.BombNumber);
        // _game.WhiteEngine.Move(_game);

        for (int i = 0; i < 30; i++)
        {
            var j = i;
            _squares[i] = Squares.transform.Find("Square (" + i.ToString() + ")").GetComponent<KMSelectable>();
            _squares[i].OnInteract += delegate () { ClickSquare(j); return false; };
        }

        Button.OnInteract += delegate () { SwitchPieces(); return false; };

        string serial = Bomb.GetSerialNumber();
        var sb = new StringBuilder();
        foreach (var c in serial)
            sb.Append((c >= '0' && c <= '9') ? (c - '0') : (c - 'A' + 1));
        Log("Bomb number: {0}", sb);

        // Try random engines and seeds until we are satisfied with the puzzle
        while (true)
        {
            List<Engine> engines = Engine.GetAllRandom();
            _game = new Game
            {
                BombNumber = sb.ToString(),
                WhiteEngine = engines[0],
                BlackEngine = engines[1]
            };
            List<int> seeds = new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }.Shuffle();
            _game.WhiteEngine.Init(Player.White, seeds[0], _game.BombNumber);
            _game.BlackEngine.Init(Player.Black, seeds[1], _game.BombNumber);
            _game.Run();

            // We want the game to be in progress and let the player play some moves for each side
            if (_game.CurrentMove < 12) continue;

            // All conditions are met
            break;
        }

        Log("{0} player: '{1}', seed: {2}, random: {3}",
            _game.WhiteEngine.Color,
            _game.WhiteEngine.Name,
            _game.WhiteEngine.Seed,
            _game.WhiteEngine.Random);
        Log("{0} player: '{1}', seed: {2}, random: {3}",
            _game.BlackEngine.Color,
            _game.BlackEngine.Name,
            _game.BlackEngine.Seed,
            _game.BlackEngine.Random);
        Log("Full game: {0}", string.Join(
            "",
            _game.Moves.Select((m, i) =>
                ((i % 2 == 0) ? ((i / 2 + 1) + ". ") : "")
                + m.GetName() + " "
            ).ToArray()));

        // Rewind a couple of moves
        _game.CurrentMove -= UnityEngine.Random.Range(4, 7);
        _game.Turn = (_game.CurrentMove % 2 == 0) ? Player.White : Player.Black;
        // Don't let "Mirror, mirror" be the first one to play, you can't see the previous move
        if ((_game.Turn == Player.White && _game.WhiteEngine.Letter == 'M')
            || (_game.Turn == Player.Black && _game.BlackEngine.Letter == 'M'))
        {
            _game.CurrentMove--;
            _game.Turn = (_game.CurrentMove % 2 == 0) ? Player.White : Player.Black;
        }
        _game.Board = new StringBuilder(_game.Boards[_game.CurrentMove]);
        _game.NextAction = _game.Turn == Player.White ? NextAction.WhiteFrom : NextAction.BlackFrom;

        Log("Next move: {0} {1}. {2}", _game.Turn, _game.CurrentMove / 2 + 1, _game.Moves[_game.CurrentMove].GetName());

        // Init pieces, display and selectables
        SwitchPieces();
        UpdateDisplay();
        SetSelectables(_game.NextAction);
    }

    private void SwitchPieces()
    {
        if (_pieces == FlatPieces)
        {
            _pieces = FullPieces;
            FullPieces.SetActive(true);
            FlatPieces.SetActive(false);
            Button.transform.Find("On").GetComponent<TextMesh>().text = "FULL\n";
            Button.transform.Find("Off").GetComponent<TextMesh>().text = "\nFLAT";
        }
        else
        {
            _pieces = FlatPieces;
            FullPieces.SetActive(false);
            FlatPieces.SetActive(true);
            Button.transform.Find("On").GetComponent<TextMesh>().text = "\nFLAT";
            Button.transform.Find("Off").GetComponent<TextMesh>().text = "FULL\n";
        }
    }

    private void Log(string message, params object[] args)
    {
        Debug.LogFormat("[Lousy Chess #" + _moduleId + "] " + message, args);
    }

    private void UpdateDisplay()
    {
        // For both full and flat pieces
        foreach (var pieces in new GameObject[] { FullPieces, FlatPieces })
        {
            // For each piece in the scene
            for (var i = 0; i < pieces.transform.childCount; i++)
            {
                // Find it on the internal board
                var piece = pieces.transform.GetChild(i);
                var index = _game.Board.ToString().IndexOf(piece.gameObject.name);

                // If it's not on the internal board, hide it in the scene
                if (index == -1)
                {
                    piece.gameObject.SetActive(false);
                }

                // Otherwise, show it and move it to the correct position
                else
                {
                    piece.gameObject.SetActive(true);
                    piece.transform.localPosition = new Vector3(index % 5, 0, -index / 5);
                }
            }
        }

        // Update the Selected square
        if (_game.NextAction == NextAction.WhiteTo || _game.NextAction == NextAction.BlackTo)
        {
            Selected.gameObject.SetActive(true);
            var index = _game.Moves[_game.CurrentMove].From;
            Selected.transform.localPosition = new Vector3(index % 5, 0, -index / 5);
        }
        else
        {
            Selected.gameObject.SetActive(false);
        }

        // Update move count
        if (!_solved)
            DisplayText.GetComponent<TextMesh>().text = String.Format(
                "{0}{1}\n{2}{3}\n{4}",
                _game.WhiteEngine.Letter.ToString(),
                _game.WhiteEngine.Seed,
                _game.BlackEngine.Letter.ToString(),
                _game.BlackEngine.Seed,
                _game.CurrentMove / 2 + 1);
    }

    private void ClickSquare(int i)
    {
        if (_game.NextAction == NextAction.WhiteFrom || _game.NextAction == NextAction.BlackFrom)
        {
            if (_game.Moves[(_game.CurrentMove)].From != i)
            {
                GetComponent<KMBombModule>().HandleStrike();
                Log("You played from {0}: Strike!", _game.SquareName(i));
            }
            else
            {
                GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
                _game.NextAction = (_game.NextAction == NextAction.WhiteFrom) ? NextAction.WhiteTo : NextAction.BlackTo;
            }
        }
        else if (_game.NextAction == NextAction.WhiteTo || _game.NextAction == NextAction.BlackTo)
        {
            if (_game.Moves[_game.CurrentMove].To != i)
            {
                GetComponent<KMBombModule>().HandleStrike();
                Log("You played to {0}: Strike!", _game.SquareName(i));
            }
            else
            {
                if (_game.CurrentMove + 1 == _game.Moves.Count)
                {
                    _solved = true;
                    _game.NextAction = NextAction.None;
                    GetComponent<KMBombModule>().HandlePass();
                }
                else
                {
                    _game.NextAction = (_game.NextAction == NextAction.WhiteTo) ? NextAction.BlackFrom : NextAction.WhiteFrom;
                    _game.Turn = (_game.NextAction == NextAction.WhiteFrom) ? Player.White : Player.Black;
                }
                if (Piece.IsPiece(_game.Boards[_game.CurrentMove - 1][_game.Moves[_game.CurrentMove].To]))
                    GetComponent<KMAudio>().PlaySoundAtTransform("capture", transform);
                else
                    GetComponent<KMAudio>().PlaySoundAtTransform("move", transform);
                _game.CurrentMove++;
                _game.Board = new StringBuilder(_game.Boards[_game.CurrentMove]);
                if (_game.CurrentMove < _game.Moves.Count)
                    Log("Next move: {0} {1}. {2}", _game.Turn, _game.CurrentMove / 2 + 1, _game.Moves[_game.CurrentMove].GetName());
            }
        }

        UpdateDisplay();
        SetSelectables(_game.NextAction);
    }

    private void SetSelectables(NextAction act)
    {
        List<KMSelectable> selectables = new List<KMSelectable>();
        switch (act)
        {
            case NextAction.None:
                break;
            case NextAction.WhiteFrom:
                for (var i = 0; i < _game.Board.Length; i++)
                    if (Piece.IsWhite(_game.Board[i]))
                        selectables.Add(Squares.transform.Find("Square (" + i + ")").GetComponent<KMSelectable>());
                break;
            case NextAction.WhiteTo:
                for (var i = 0; i < _game.Board.Length; i++)
                    if (!Piece.IsWhite(_game.Board[i]))
                        selectables.Add(Squares.transform.Find("Square (" + i + ")").GetComponent<KMSelectable>());
                break;
            case NextAction.BlackFrom:
                for (var i = 0; i < _game.Board.Length; i++)
                    if (Piece.IsBlack(_game.Board[i]))
                        selectables.Add(Squares.transform.Find("Square (" + i + ")").GetComponent<KMSelectable>());
                break;
            case NextAction.BlackTo:
                for (var i = 0; i < _game.Board.Length; i++)
                    if (!Piece.IsBlack(_game.Board[i]))
                        selectables.Add(Squares.transform.Find("Square (" + i + ")").GetComponent<KMSelectable>());
                break;
        }
        selectables.Add(Button);
        Module.Children = selectables.ToArray();
        Module.UpdateChildren();
    }

    //Twitch Plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} Press A2 B5 C4 to select the squares at A2, B5, and C4 in chess coordinates. (If they are selectable.) If the square is empty, the module will do nothing and will process the subsequent squares. Use !{0} switch to switch between full and flat chess set.";
    #pragma warning restore 414

    public IEnumerator TwitchHandleForcedSolve()
    {
        while(!_solved)
        {
            if (_game.NextAction == NextAction.WhiteFrom || _game.NextAction == NextAction.BlackFrom)
            {
                _squares[_game.Moves[(_game.CurrentMove)].From].OnInteract();
            }
            else if (_game.NextAction == NextAction.WhiteTo || _game.NextAction == NextAction.BlackTo)
            {
                _squares[_game.Moves[_game.CurrentMove].To].OnInteract();
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    public IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ');
        int inputType = 0;
        /* Validating the command */
        if (Regex.IsMatch(parameters[0], @"^\s*switch\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) && parameters.Length == 1)
        {
            inputType = 1;
        }
        else if (Regex.IsMatch(parameters[0], @"^\s*press\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            for (int i = 1; i < parameters.Length; i++)
            {
                if (Regex.IsMatch(parameters[i], @"^\s*[a-e][1-6]\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                {
                    parameters[i] = parameters[i].Trim();
                    parameters[i] = parameters[i].ToLowerInvariant();
                }
                else
                {
                    yield return "sendtochaterror Coordinates must be in the format [Alphabet][Digit] where Alphabet is English alphabet from A - E and Digit is a single number 1 - 6.";
                    yield break;
                }
            }
            inputType = 2;
        }
        else
        {
            yield return "sendtochaterror The command must be started with \"switch\" or \"press\". \"switch\" must not be followed by any other letter or number.";
            yield break;
        }

        if (inputType == 1)
        {
            Button.OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        else if (inputType == 2)
        {
            var position = new int[parameters.Length - 1];

            for (int i = 1; i < parameters.Length; i++)
            {
                position[i - 1] = (parameters[i][0] - 'a') + (5 * (5 - (parameters[i][1] - '1')));
            }

            yield return null;

            for (int i = 0; i < position.Length; i++)
            {
                if (Module.Children.Any(selectable => selectable == _squares[position[i]]))
                {
                    _squares[position[i]].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
            }
        }
        
        yield break;
    }
}
