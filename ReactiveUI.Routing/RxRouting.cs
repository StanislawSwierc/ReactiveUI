﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using ReactiveUI;

namespace ReactiveUI.Routing
{
    public static class RxRouting
    {
        public static Func<string, string> ViewModelToViewFunc { get; set; }

        static RxRouting()
        {
            ViewModelToViewFunc = (vm) => 
                vm.Replace("ViewModel", "View");
        }

        /// <summary>
        /// Returns the View associated with a ViewModel, deriving the name of
        /// the Type via ViewModelToViewFunc, then discovering it via
        /// ServiceLocator.
        /// </summary>
        /// <param name="viewModel">The ViewModel for which to find the
        /// associated View.</param>
        /// <returns>The View for the ViewModel.</returns>
        public static IViewForViewModel ResolveView<T>(T viewModel)
            where T : IReactiveNotifyPropertyChanged
        {
            var attrs = viewModel.GetType().GetCustomAttributes(typeof (ViewContractAttribute), true);
            string key = null;

            if (attrs.Count() > 0) {
                key = ((ViewContractAttribute) attrs.First()).Contract;
            }

            var viewType = typeof (IViewForViewModel<>);
            return (IViewForViewModel) RxApp.GetService(viewType.MakeGenericType(viewModel.GetType()), key);
        }
    }

    public static class RoutableViewModelMixin
    {
        /// <summary>
        /// This Observable fires whenever the current ViewModel is navigated to.
        /// </summary>
        public static IObservable<Unit> NavigatedToMe(this IRoutableViewModel This)
        {
            return Observable.Create<Unit>(subj => {
                return This.HostScreen.Router.ViewModelObservable()
                    .Where(x => x == This)
                    .Select(_ => Unit.Default)
                    .Subscribe(subj);
            });
        }
    }
}
