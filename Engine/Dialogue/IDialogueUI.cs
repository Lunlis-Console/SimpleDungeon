using System;
using System.Collections.Generic;

namespace SimpleDungeon.Engine.Dialogue
{
    // Интерфейс UI, который использует раннер.
    // Реализуйте его в вашей игре (WinForms/Unity/Console) чтобы показывать узел и варианты.
    public interface IDialogueUI
    {
        // Показывает текущий узел и список видимых ответов
        void ShowNode(DialogueNode node);

        // Закрывает диалог
        void CloseDialogue();

        // Может логировать/показывать сообщения
        void Log(string message);
    }
}
