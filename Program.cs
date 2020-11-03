using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MineSweeper
{
    class Program
    {
        static void Main(string[] args)
        {
            var board = new Board(9, 9, 10);
            
            while (true)
            {
                Console.WriteLine("Write quit to leave or the coordinates row,col");
                var response = Console.ReadLine(); 
                if(response == "" || response == "quit")
                {
                    break;
                }
                else
                {
                    var arrResponse = response.Split(','); 
                    if(arrResponse.Length != 2)
                    {
                        break; 
                    }
                    var row = Int32.Parse(arrResponse[0]);
                    var col = Int32.Parse(arrResponse[1]);
                    board.Select(row, col);
                    board.PrintBoard(false); 
                    if(board.Status == Constants.GameStatus.Ended)
                    {
                        Console.WriteLine("Game Ended");
                        board.PrintBoard(true);
                        break; 
                    }
                }
            }            
            Console.WriteLine("Process done");
            Console.ReadLine();
        }
    }

    internal static class Constants
    {
        public const int MineValue = 9; 
        public const int UnAssigned = -1;
        public const char HiddenChar = '-'; 
        public enum GameStatus
        {
            None = 0, 
            Playing = 1, 
            Ended = 2
        }
    }

    internal class Field
    {
        public int Value { get; set;  }
        public int Row { get; internal set; }
        public int Col { get; internal set; }
        public bool Revealed { get; internal set; }

        public bool HasMine()
        {
            return Value == Constants.MineValue; 
        }

        public void AssignMine()
        {
            Value = Constants.MineValue; 
        }

        public bool IsUnassigned()
        {
            return Value == Constants.UnAssigned;
        }

        public void SetAsRevealed()
        {
            Revealed = true; 
        }


        public Field(int row, int col)
        {
            Row = row;
            Col = col;
            Revealed = false;
            Value = Constants.UnAssigned; 
        }

        internal void SetMineCount(int mineCount)
        {
            Value = mineCount; 
        }
    }


    internal class Board
    {
        public int Rows { get; internal set; }
        public int Cols { get; internal set; }
        public int TotalFields { get { return Rows * Cols; } }
        public int MineCount { get; internal set;  }
        public Field[,] FieldList; 
        public Constants.GameStatus Status { get; private set; }
        public int MoveCount { get; private set;  }
        

        public Board(int rows, int cols, int totalMinesToAllocate)
        {
            FieldList = new Field[rows,cols];
            Rows = rows;
            Cols = cols; 
            if(totalMinesToAllocate > TotalFields)
            {
                throw new Exception("There can't be more mines than fields allocated "); 
            }else if(totalMinesToAllocate < 1)
            {
                throw new Exception("There has to be at least one mine ");
            }

            Status = Constants.GameStatus.Playing;
            MoveCount = 0; 



            AssignInitialValues(); 
            AssignMines(totalMinesToAllocate);
            CalculateFieldValues();
        }

        private void AssignInitialValues()
        {
            for (var currentRow = 0; currentRow < Rows; currentRow++)
            {
                for (var currentCol = 0; currentCol < Cols; currentCol++)
                {
                    FieldList[currentRow, currentCol] = new Field(currentRow, currentCol);
                }
            }
        }

        private void CalculateFieldValues()
        {
            for(int currentRow = 0; currentRow < Rows; currentRow++)
            {
                for(int currentCol = 0; currentCol < Cols; currentCol++)
                {
                    var field = Get(currentRow, currentCol);
                    if (!field.HasMine())
                    {
                        var neighborList = GetNeighbors(currentRow, currentCol);
                        var mineCount = 0;
                        foreach (var neighbor in neighborList)
                        {
                            if (neighbor != null && neighbor.HasMine())
                            {
                                mineCount++;
                            }
                        }
                        field.SetMineCount(mineCount);
                    }
                }
            }            
        }

        private void AssignMines(int minesToAllocate)
        {
            var shuffledStack = CreateShuffledStack(this.FieldList); 
            while(minesToAllocate > 0)
            {
                var currentField = shuffledStack.Pop();
                currentField.AssignMine();
                minesToAllocate--; 
            }
        }


        private static Stack<Field> CreateShuffledStack(Field[,] fieldList)
        {
            var random = new Random();
            var list = new List<Field>();
            var stack = new Stack<Field>(); 
            for(var currentRow = 0; currentRow < fieldList.GetLength(0); currentRow++)
            {
                for(var currentCol = 0; currentCol < fieldList.GetLength(1); currentCol++)
                {
                    list.Add(fieldList[currentRow, currentCol]); 
                }
            }
            while(list.Count > 0)
            {
                var randomIndex = random.Next(0, list.Count);
                var randomItem = list[randomIndex];
                list.RemoveAt(randomIndex);
                stack.Push(randomItem); 
            }
            return stack; 
        }

        

        private List<Field> GetNeighbors(int row, int col)
        {
            var neighbors = new List<Field>();
            var currentField = FieldList[row, col]; 
            if(currentField == null)
            {
                throw new Exception($"Field not found {row} {col}"); 
            }
            neighbors.Add(Get(currentField.Row - 1, currentField.Col));
            neighbors.Add(Get(currentField.Row - 1, currentField.Col - 1));
            neighbors.Add(Get(currentField.Row - 1, currentField.Col + 1));
            
            neighbors.Add(Get(currentField.Row, currentField.Col - 1));
            neighbors.Add(Get(currentField.Row, currentField.Col + 1));

            neighbors.Add(Get(currentField.Row + 1, currentField.Col));
            neighbors.Add(Get(currentField.Row + 1, currentField.Col - 1));
            neighbors.Add(Get(currentField.Row + 1, currentField.Col + 1));


            return neighbors.Where(x => x != null).ToList(); 

        }

        private Field Get(int row, int col)
        {
            if (row >= Rows || (row < 0))
                return null;
            if (col >= Cols || (col < 0))
                return null;

            return FieldList[row, col]; 
        }

        public void PrintBoard(bool showHidden = false)
        {
            
            for(var currentRow = 0; currentRow < Rows; currentRow++)
            {
                for(var currentCol= 0; currentCol < Cols; currentCol++)
                {
                    var field = FieldList[currentRow, currentCol];
                    var value = FieldList[currentRow, currentCol].Value.ToString(); 
                    if (!showHidden && !field.Revealed)
                    {
                        value = Constants.HiddenChar.ToString(); 
                    }                    

                    Console.Write($"{value.PadLeft(2,' ')} ");
                }
                Console.WriteLine(); 
            }
        }

        internal void Select(int row, int col)
        {
            var field = Get(row, col); 
            if(field == null)
            {
                Console.WriteLine("Invalid field");
                return; 
            }

            if (field.HasMine())
            {
                this.Status = Constants.GameStatus.Ended;
            }
            else
            {
                field.SetAsRevealed();
                Queue<Field> queue = new Queue<Field>();
                var neighbors = GetNeighbors(field.Row, field.Col); 
                foreach(var currentNeighbor in neighbors)
                {
                    if (!currentNeighbor.HasMine())
                    {
                        queue.Enqueue(currentNeighbor); 
                    }
                }
                var processedItems = new List<Field>(); 
                while(queue.Count > 0)
                {
                    var currentField = queue.Dequeue();
                    currentField.SetAsRevealed();
                    processedItems.Add(currentField); 
                    
                    if(currentField.Value == 0)
                    {
                        var currentFieldNeighbors = GetNeighbors(currentField.Row, currentField.Col); 
                        foreach(var item in currentFieldNeighbors)
                        {
                            if(!item.HasMine() && (!processedItems.Contains(item)))
                            {
                                queue.Enqueue(item); 
                            }
                        }
                    }
                }
                
            }

            
        }
    }
}
