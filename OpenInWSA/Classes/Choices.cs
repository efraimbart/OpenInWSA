using System;
using System.Collections.Generic;
using System.Linq;
using OpenInWSA.Extensions;

namespace OpenInWSA.Classes
{
    public class Choices<T> : List<Choices<T>.IChoice<T>>
    {
        private string Question { get; set; }
        private int? DefaultChoice { get; set; }
        
        public Choices(string question)
        {
            Question = question;
        }
        
        public Choices(string question, IEnumerable<KeyValuePair<string, T>> choices) : this(question)
        {
            AddRange(choices.Select(choice => new Choice(choice)));
        }

        public Choices<T> AddRange<TAdd>(IEnumerable<TAdd> values, Func<TAdd, string> getText) where TAdd : T
        {
            AddRange(values.Select(choice => (IChoice<T>)new Choice<TAdd>(choice, getText)));
            return this;
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

        public IChoice<T> Choose()
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
        
        public interface IChoice<out TChoice> where TChoice : T
        {
            string Text { get; }
            TChoice Value { get; }
        }
        
        public class Choice : Choice<T>
        {
            public Choice() : base()
            {
            }

            public Choice(KeyValuePair<string, T> choice) : base(choice)
            {
            }

            public Choice(T choice, Func<T, string> getText) : base(choice, getText)
            {
            }
        }

        public class Choice<TChoice> : IChoice<TChoice> where TChoice : T
        {
            public string Text { get; set; }
            public TChoice Value { get; set; }

            public Choice()
            {
            }

            public Choice(KeyValuePair<string, TChoice> choice)
            {
                (Text, Value) = choice;
            }
            
            public Choice(TChoice choice, Func<TChoice, string> getText)
            {
                Text = getText(choice);
                Value = choice;
            }
        }
    }
}