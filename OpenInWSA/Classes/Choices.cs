using System;
using System.Collections.Generic;
using System.Linq;
using OpenInWSA.Extensions;

namespace OpenInWSA.Classes
{
    public class Choices<T> : List<Choices<T>.Choice>
    {
        private string Question { get; set; }
        private int? DefaultChoice { get; set; }
        
        public Choices(string question)
        {
            Question = question;
        }
        
        public Choices<T> Add(string text, T value, bool defaultChoice = false, bool condition = true)
        {
            if (condition)
            {
                if (defaultChoice)
                {
                    DefaultChoice = Count;
                }

                Add(new Choice {Text = text, Value = value});
            }
            return this;
        }

        public Choices<T> Add(T value, bool defaultChoice = false, bool condition = true)
        {
            return Add(value.ToString(), value, defaultChoice, condition);
        }

        public Choices<T> Default(int? index)
        {
            DefaultChoice = index;
            return this;
        }

        public Choice Choose()
        {
            var defaultChoice = this.ElementAtOrDefault(DefaultChoice);
            var questionWithDefault = defaultChoice != null 
                ? $"{Question} [{defaultChoice.Text}]"
                : Question;

            Console.WriteLine(questionWithDefault);
            
            for (var i = 0; i < Count; i++)
            {
                var choice = this[i];
                Console.WriteLine($@"[{i + 1}] {choice.Text}");
            }

            var chosenString = Console.ReadLine();
            Console.WriteLine();

            if (string.IsNullOrWhiteSpace(chosenString))
            {
                return this.ElementAtOrDefault(DefaultChoice);
            }
            
            var chosen = this.FirstOrDefault(x => x.Text.Equals(chosenString, StringComparison.InvariantCultureIgnoreCase));
            if (chosen != null)
            {
                return chosen;
            }

            if (int.TryParse(chosenString, out var chosenNumber))
            {
                return this.ElementAtOrDefault(chosenNumber - 1);
            }

            return null;
        }

        public class Choice
        {
            public string Text { get; set; }
            public T Value { get; set; }
        }
    }

    public class Choices : Choices<string>
    {
        public Choices(string question, IEnumerable<string> collection) : base(question)
        {
            foreach (var choice in collection)
            {
                Add(choice);
            }
        }
    }
}