using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.Serialization;
using ReactiveUI.Xaml;

namespace ReactiveUI.Routing
{
    /// <summary>
    /// RoutingState manages the ViewModel Stack and allows ViewModels to
    /// navigate to other ViewModels.
    /// </summary>
    [DataContract]
    public class RoutingState : ReactiveObject, IRoutingState
    {
        [IgnoreDataMember] ReactiveCollection<IRoutableViewModel> _NavigationStack;

        /// <summary>
        /// Represents the current navigation stack, the last element in the
        /// collection being the currently visible ViewModel.
        /// </summary>
        [DataMember]
        public ReactiveCollection<IRoutableViewModel> NavigationStack {
            get { return _NavigationStack; }
            protected set { _NavigationStack = value; }
        }

        /// <summary>
        /// Navigates back to the previous element in the stack.
        /// </summary>
        [IgnoreDataMember]
        public IReactiveCommand NavigateBack { get; protected set; }

        /// <summary>
        /// Navigates to the a new element in the stack - the Execute parameter
        /// must be a ViewModel that implements IRoutableViewModel.
        /// </summary>
        [IgnoreDataMember]
        public IReactiveCommand Navigate { get; protected set; }

        /// <summary>
        /// Navigates to a new element and resets the navigation stack (i.e. the
        /// new ViewModel will now be the only element in the stack) - the
        /// Execute parameter must be a ViewModel that implements
        /// IRoutableViewModel.
        /// </summary>
        [IgnoreDataMember]
        public IReactiveCommand NavigateAndReset { get; protected set; }

        /// <summary>
        /// The currently visible ViewModel.
        /// </summary>
        [IgnoreDataMember]
        public IObservable<IRoutableViewModel> CurrentViewModel { get; protected set; }

        public RoutingState()
        {
            _NavigationStack = new ReactiveCollection<IRoutableViewModel>();
            setupRx();
        }

        [OnDeserialized]
        void setupRx(StreamingContext sc) { setupRx();  }
        void setupRx()
        {
            NavigateBack = new ReactiveCommand(
                NavigationStack.CollectionCountChanged.StartWith(_NavigationStack.Count).Select(x => x > 0));
            NavigateBack.Subscribe(_ =>
                NavigationStack.RemoveAt(NavigationStack.Count - 1));

            Navigate = new ReactiveCommand();
            Navigate.Subscribe(x =>
                NavigationStack.Insert(NavigationStack.Count, (IRoutableViewModel)x));

            NavigateAndReset = new ReactiveCommand();
            NavigateAndReset.Subscribe(x => {
                NavigationStack.Clear();
                NavigationStack.Add((IRoutableViewModel) x);
            });
        }
    }

    public static class RoutingStateMixins
    {
        /// <summary>
        /// Generates a routing Uri based on the current route state
        /// </summary>
        /// <returns></returns>
        public static string GetUrlForCurrentRoute(this IRoutingState This)
        {
            return "app://" + String.Join("/", This.NavigationStack.Select(x => x.UrlPathSegment));
        }

        /// <summary>
        /// Locate the first ViewModel in the stack that matches a certain Type.
        /// </summary>
        /// <returns>The matching ViewModel or null if none exists.</returns>
        public static T FindViewModelInStack<T>(this IRoutingState This)
            where T : IRoutableViewModel
        {
            return This.NavigationStack.Reverse().OfType<T>().FirstOrDefault();
        }

        /// <summary>
        /// Returns the currently visible ViewModel
        /// </summary>
        public static IRoutableViewModel GetCurrentViewModel(this IRoutingState This)
        {
            return This.NavigationStack.LastOrDefault();
        }

        /// <summary>
        /// Returns an Observable that signals ViewModel changes. This is a
        /// Replay Observable, so Subscribing to it will always produce a
        /// (possibly null) initial value.
        /// </summary>
        public static IObservable<IRoutableViewModel> ViewModelObservable(this IRoutingState This)
        {
            return This.NavigationStack.CollectionCountChanged
                .Select(_ => This.GetCurrentViewModel())
                .StartWith(Scheduler.Immediate, This.GetCurrentViewModel())
                .Multicast(new ReplaySubject<IRoutableViewModel>(1)).RefCount();
        }
    }
}
