﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UltimatePathEditor.ViewContract;
using UltimatePathEditor.Model;
using System.Collections.Specialized;
using System.Windows.Input;

namespace UltimatePathEditor.ViewModel
{
    /// <summary>
    /// Model of Path Variable View
    /// </summary>
    class PathVariableViewModel : BindableBase, IPathVariableViewContract
    {
        #region Fields
        private ObservableCollection<IPathValueViewContract> _pathValues = new ObservableCollection<IPathValueViewContract>();
        private bool _modifyState = false;
        private RelayCommand _purgeCommand;
        private RelayCommand _undoCommand;
        private RelayCommand _redoCommand;
        #endregion Fields

        #region Properties
        public ObservableCollection<IPathValueViewContract> PathValues
        {
            get { return this._pathValues; }
        }

        public ICommand PurgeCommand { get { return this._purgeCommand; } }

        public ICommand UndoCommand { get { return this._undoCommand; } }

        public ICommand RedoCommand { get { return this._redoCommand; } }
        #endregion Properties

        public PathVariableViewModel()
        {
            this._purgeCommand = new RelayCommand((o) => this.PurgeUnvalidPathValue());
            this._undoCommand = new RelayCommand((o) => this.Undo());
            this._redoCommand = new RelayCommand((o) => this.Redo());
            this._pathValues.CollectionChanged += PathValues_CollectionChanged;
            Refresh();
        }

        /// <summary>
        /// Refresh the list of Path Value
        /// </summary>
        public void Refresh()
        {
            Refresh(PathVariableManager.Instance.GetEnvironmentVariable());
        }

        /// <summary>
        /// Refresh the list of Path Value
        /// </summary>
        public void Refresh(string pathEnvironmentVariable)
        {
            _modifyState = true;
            this._pathValues.Clear();
            var pathValues = pathEnvironmentVariable.Split(PathVariableManager.SplitCharacter);
            foreach (var pathValue in pathValues)
            {
                this._pathValues.Add(new PathValueViewModel { Value = pathValue });
            }
            _modifyState = false;
        }

        /// <summary>
        /// Send the Environment Varaible Path to DAL
        /// </summary>
        private void SendEnvironmentVariable()
        {
            var tmp = string.Empty;
            foreach (var pathValue in _pathValues)
                tmp += pathValue.Value + PathVariableManager.SplitCharacter;
            PathVariableManager.Instance.SetEvnironmentVariableMemento(tmp);
        }

        /// <summary>
        /// Remove Unvalid path value
        /// </summary>
        private void PurgeUnvalidPathValue()
        {
            this._modifyState = true;
            int previousCount = this._pathValues.Count;
            int i = 0;
            while(i<this._pathValues.Count)
            {
                if (this._pathValues[i].IsValid)
                    i++;
                else
                    this._pathValues.RemoveAt(i);
            }
            this._modifyState = false;
            this.SendEnvironmentVariable();
        }

        /// <summary>
        /// Undo a modification of the Environment Variable Path
        /// </summary>
        private void Undo()
        {
            var result = PathVariableManager.Instance.Undo();
            if (result != null)
                Refresh(result);
        }

        /// <summary>
        /// Redo a modification of the Environment Variable Path
        /// </summary>
        private void Redo()
        {
            var result = PathVariableManager.Instance.Redo();
            if (result != null)
                Refresh(result);
        }

        #region Subscribe
        void PathValues_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch(e.Action)
            {
                case NotifyCollectionChangedAction.Add :
                    foreach(PathValueViewModel pathValue in e.NewItems)
                    {
                        pathValue.PropertyChanged += PathValueViewModel_PropertyChanged;
                    }
                    break;
            }
        }

        void PathValueViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var PathValue = sender as PathValueViewModel;
            if(PathValue != null && e.PropertyName == "Value")
            {
                bool ownerModifyState = !_modifyState; //Responsabilty to modify Path Environment Variable at end of operation
                _modifyState = true;
                if (String.IsNullOrEmpty(PathValue.Value))
                {
                    _pathValues.Remove(PathValue);
                }
                else
                {
                    var arrayPathValue = PathValue.Value.Split(PathVariableManager.SplitCharacter);
                    if (arrayPathValue.Length > 1)
                    {
                        PathValue.Value = arrayPathValue.First();
                        int baginIndex = _pathValues.IndexOf(PathValue);
                        for (int i = 1; i < arrayPathValue.Length; i++)
                            _pathValues.Insert(baginIndex + i, new PathValueViewModel { Value = arrayPathValue[i] });
                    }
                }
                if (ownerModifyState)
                {
                    this.SendEnvironmentVariable();
                    _modifyState = false;
                }
            }
        }
        #endregion Subscribe

        #region Drag and Drop
        public bool Drag(IPathValueViewContract pathValue)
        {
            return this._pathValues.Contains(pathValue);
        }

        public void Drop(IPathValueViewContract pathValue, IPathValueViewContract target)
        {
            if (pathValue != target)
                if (this._pathValues.Remove(pathValue))
                    this._pathValues.Insert(this._pathValues.IndexOf(target) + 1, pathValue);
        }
        #endregion Drag and Drop
    }
}
