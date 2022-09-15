/* Game.cs
 * Author: Rod Howell
 * 
 * Edited By: Ian Flores
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace Ksu.Cis300.Klondike
{
    /// <summary>
    /// The game controller.
    /// </summary>
    public class Game
    {
        /// <summary>
        /// The random number generator.
        /// </summary>
        private Random _randomNumbers;

        /// <summary>
        /// keeps track of discard pile
        /// </summary>
        private DiscardPile _discardPile = null;

        /// <summary>
        /// keeps track of the tableau column
        /// </summary>
        private TableauColumn _tableauColumn = null;

        /// <summary>
        /// holds the number of face down tableau cards are on the board
        /// </summary>
        private int _numFaceDownTableauCards = 21;

        /// <summary>
        /// holds the number of face up tableau cards are on the board
        /// </summary>
        private int _numFaceDownStockCards = 24;

        /// <summary>
        /// Gets a new card deck.
        /// </summary>
        /// <returns>The new card deck.</returns>
        private Card[] GetNewDeck()
        {
            Card[] cards = new Card[52];
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i] = new Card(i % 13 + 1, (Suit)(i / 13));
            }
            return cards;
        }

        /// <summary>
        /// Shuffles a new deck and pushes the cards onto the given stack.
        /// </summary>
        /// <param name="shuffled">The stack on which to push the cards.</param>
        private void ShuffleNewDeck(Stack<Card> shuffled)
        {
            Card[] deck = GetNewDeck();
            for (int i = deck.Length - 1; i >= 0; i--)
            {
                // Get a random nonnegative integer less than or equal to i.
                int j = _randomNumbers.Next(i + 1);

                shuffled.Push(deck[j]);
                deck[j] = deck[i];
            }
        }

        /// <summary>
        /// Constructs a new game from the given controls and seed.
        /// </summary>
        /// <param name="stock">The stock.</param>
        /// <param name="tableau">The tableau columns.</param>
        /// <param name="seed">The random number seed. If -1, no seed is used.</param>
        public Game(CardPile stock, TableauColumn[] tableau, int seed)
        {
            if (seed == -1)
            {
                _randomNumbers = new Random();
            }
            else
            {
                _randomNumbers = new Random(seed);
            }
            ShuffleNewDeck(stock.Pile);
            DealCards(stock.Pile, tableau);
        }

        /// <summary>
        /// Draws the next three cards from the stock, or returns the discard pile to the stock
        /// if the stock is empty.
        /// </summary>
        /// <param name="stock">The stock.</param>
        /// <param name="discard">The discard pile.</param>
        public void DrawCardsFromStock(CardPile stock, DiscardPile discard)
        {
            if (stock.Pile.Count > 0)
            {
                for (int i = 0; i < Math.Min(3, stock.Pile.Count); i++)
                {
                    discard.Pile.Push(stock.Pile.Pop());
                }
            }
            else
            {
                for (int i = 0; i < discard.Pile.Count; i++)
                {
                    stock.Pile.Push(discard.Pile.Pop());
                }
            }
        }

        /// <summary>
        /// Selects the top discarded card, or removes the selection if there already is one.
        /// </summary>
        /// <param name="discard">The discard pile.</param>
        public void SelectDiscard(DiscardPile discard)
        {
            if (_discardPile == null || _tableauColumn == null)
            {
                _discardPile = discard;
                _discardPile.IsSelected = true;
            }
            else
            {
                RemoveSelection();
            }
        }

        /// <summary>
        /// Selects the given number of cards from the given tableau column or tries to move
        /// any currently-selected cards to the given tableau column.
        /// </summary>
        /// <param name="col">The column to select or to move cards to.</param>
        /// <param name="n">The number of cards to select.</param>
        /// <returns>Whether the play wins the game.</returns>
        public bool SelectTableauCards(TableauColumn col, int n)
        {
            if (_discardPile == null && _tableauColumn == null)
            {
                _tableauColumn = col;
                _tableauColumn.NumberSelected = n;
            }
            else if (_discardPile != null)
            {
                if (n <= 1)
                {
                    DiscardToTableau(col.FaceUpPile);
                }
                RemoveSelection();
            }
            else if (_tableauColumn != null)
            {
                if (n <= 1)
                {
                    TableauToTableau(col.FaceUpPile);
                }
                RemoveSelection();
            }
            return IsWon();
        }

        /// <summary>
        /// Moves the selected card to the given foundation pile, if possible
        /// </summary>
        /// <param name="dest">The foundation pile.</param>
        /// <returns>Whether the move wins the game.</returns>
        public bool MoveSelectionToFoundation(Stack<Card> dest)
        {
            TableauColumn tableau = new TableauColumn();
            if (_tableauColumn != null)
            {
                TableauToFoundation(dest);
                RemoveSelection();
            }
            if (_discardPile != null)
            {
                DiscardToFoundation(dest);
                RemoveSelection();
            }
            return IsWon();
        }

        /// <summary>
        /// moves cards from one stack to another
        /// </summary>
        /// <param name="originalStack">The stack that the cards are being taken from</param>
        /// <param name="newStack">The stack that the cards are being moved to</param>
        /// <param name="numCards">The number of cards to move</param>
        private void TransferCards(Stack<Card> originalStack, Stack<Card> newStack, int numCards)
        {
            for (int i = 0; i < numCards; i++)
            {
                newStack.Push(originalStack.Pop());
            }
        }

        /// <summary>
        /// Deselects any selected cards
        /// </summary>
        private void RemoveSelection()
        {
            if (_discardPile != null)
            {
                _discardPile.IsSelected = false;
                _discardPile = null;
            }
            if (_tableauColumn != null)
            {
                _tableauColumn.NumberSelected = 0;
                _tableauColumn = null;
            }
        }

        /// <summary>
        /// Tells if the card can be placed in a tableau column
        /// </summary>
        /// <param name="card">The card to be placed</param>
        /// <param name="stack">The stack the card is to be placed in</param>
        /// <returns>If the card can be placed in the stack</returns>
        private bool CanBePlaced(Card card, Stack<Card> stack)
        {
            if (stack.Count() != 0)
            {
                if (card.Rank == stack.Peek().Rank - 1 && (card.IsRed != stack.Peek().IsRed))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (card.Rank == 13)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Tells if the card can be placed in a foundation column
        /// </summary>
        /// <param name="card">The card to be placed</param>
        /// <param name="stack">The stack the card is to be placed in</param>
        /// <returns></returns>
        private bool CanBePlacedOnFoundationPile(Card card, Stack<Card> stack)
        {
            if (stack.Count() == 0)
            {
                if (card.Rank == 1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if ((card.Rank == stack.Peek().Rank + 1) && card.Suit == stack.Peek().Suit)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        /// Checks if the game is won
        /// </summary>
        /// <returns>If the game has been won or not</returns>
        private bool IsWon()
        {
            DiscardPile cards = new DiscardPile();
            if (_numFaceDownTableauCards == 0 && _numFaceDownStockCards <= 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Flips face down cards to be face up and decrements the number of face down cards
        /// </summary>
        /// <param name="column">the tableau column</param>
        private void FlipTableauColumnCard(TableauColumn column)
        {
            if (column.FaceUpPile.Count() == 0 && column.FaceDownPile.Count() != 0)
            {
                column.FaceUpPile.Push(column.FaceDownPile.Pop());
                _numFaceDownTableauCards--;
            }
        }

        /// <summary>
        /// Deals all the cards from the deck in the correct order
        /// </summary>
        /// <param name="deck">the deck the cards are coming from</param>
        /// <param name="columns">the columns of cards on the table</param>
        private void DealCards(Stack<Card> deck, TableauColumn[] columns)
        {
            for (int i = 0; i < 7; i++)
            {
                TransferCards(deck, columns[i].FaceUpPile, 1);
                for (int j = i + 1; j < 7; j++)
                {
                    TransferCards(deck, columns[j].FaceDownPile, 1);
                }
            }
        }

        /// <summary>
        /// Moves cards from the discard pile to the tableau column
        /// </summary>
        /// <param name="pile">The destination for the cards to be moved to</param>
        private void DiscardToTableau(Stack<Card> pile)
        {
            if (CanBePlaced(_discardPile.Pile.Peek(), pile) == true)
            {
                TransferCards(_discardPile.Pile, pile, 1);
                _numFaceDownStockCards--;
            }
        }

        /// <summary>
        /// Moves cards from the discard pile to the foundation piles
        /// </summary>
        /// <param name="foundationPile">The destination for the cards to be moved to</param>
        private void DiscardToFoundation(Stack<Card> foundationPile)
        {
            if (CanBePlacedOnFoundationPile(_discardPile.Pile.Peek(), foundationPile) == true)
            {
                TransferCards(_discardPile.Pile, foundationPile, 1);
                _numFaceDownStockCards--;
            }
        }

        /// <summary>
        /// Moves cards from one tableau column to another
        /// </summary>
        /// <param name="newTableau">The destination for the cards to be moved to</param>
        private void TableauToTableau(Stack<Card> newTableau)
        {
            Stack<Card> temp = new Stack<Card>();
            TransferCards(_tableauColumn.FaceUpPile, temp, _tableauColumn.NumberSelected);
            if (CanBePlaced(temp.Peek(), newTableau))
            {
                TransferCards(temp, newTableau, _tableauColumn.NumberSelected);
                FlipTableauColumnCard(_tableauColumn);
            }
            else
            {
                TransferCards(temp, _tableauColumn.FaceUpPile, _tableauColumn.NumberSelected);
            }
        }

        /// <summary>
        /// Moves cards from the tableau columns to the foundation piles
        /// </summary>
        /// <param name="foundation">The destination for the cards to be moved to</param>
        private void TableauToFoundation(Stack<Card> foundation)
        {
            TableauColumn tableauColumn = new TableauColumn();
            if (CanBePlacedOnFoundationPile(_tableauColumn.FaceUpPile.Peek(), foundation) && _tableauColumn.NumberSelected == 1)
            {
                TransferCards(_tableauColumn.FaceUpPile, foundation, 1);
                FlipTableauColumnCard(_tableauColumn);
                RemoveSelection();
            }
        }
    }
}
