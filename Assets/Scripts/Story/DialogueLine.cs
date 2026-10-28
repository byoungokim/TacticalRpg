namespace Tactics.Story
{
    public class DialogueLine
    {
        public readonly string Speaker;
        public readonly string Text;

        public DialogueLine(string speaker, string text)
        {
            Speaker = speaker;
            Text = text;
        }
    }
}
