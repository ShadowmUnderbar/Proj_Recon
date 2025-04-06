using UnityEngine;

namespace App.Common.Interface.Views
{
    public interface IForwardRayView
    {
        void View();
        void Close();
        void SetRayColor(Color color);
    }
}