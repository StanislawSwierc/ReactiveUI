﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reactive.Concurrency;

namespace ReactiveUI
{
    /// <summary>
    /// IObservedChange is a generic interface that replaces the non-generic
    /// PropertyChangedEventArgs. Note that it is used for both Changing (i.e.
    /// 'before change') and Changed Observables. In the future, this interface
    /// will be Covariant which will allow simpler casting between specific and
    /// generic changes.
    /// </summary>
    public interface IObservedChange<TSender, TValue>
    {
        /// <summary>
        /// The object that has raised the change.
        /// </summary>
        TSender Sender { get; }

        /// <summary>
        /// The name of the property that has changed on Sender.
        /// </summary>
        string PropertyName { get; }

        /// <summary>
        /// The value of the property that has changed. IMPORTANT NOTE: This
        /// property is often not set for performance reasons, unless you have
        /// explicitly requested an Observable for a property via a method such
        /// as ObservableForProperty. To retrieve the value for the property,
        /// use the Value() extension method.
        /// </summary>
        TValue Value { get; }
    }

    public class ObservedChange<TSender, TValue> : IObservedChange<TSender, TValue>
    {
        public TSender Sender { get; set; }
        public string PropertyName { get; set; }
        public TValue Value { get; set; }
    }

    /// <summary>
    /// IReactiveNotifyPropertyChanged represents an extended version of
    /// INotifyPropertyChanged that also exposes Observables.
    /// </summary>
    public interface IReactiveNotifyPropertyChanged : INotifyPropertyChanged, INotifyPropertyChanging, IEnableLogger
    {
        /// <summary>
        /// Represents an Observable that fires *before* a property is about to
        /// be changed. Note that this should not fire duplicate change notifications if a
        /// property is set to the same value multiple times.
        /// </summary>
        IObservable<IObservedChange<object, object>> Changing { get; }

        /// <summary>
        /// Represents an Observable that fires *after* a property has changed.
        /// Note that this should not fire duplicate change notifications if a
        /// property is set to the same value multiple times.
        /// </summary>
        IObservable<IObservedChange<object, object>> Changed { get; }

        /// <summary>
        /// When this method is called, an object will not fire change
        /// notifications (neither traditional nor Observable notifications)
        /// until the return value is disposed.
        /// </summary>
        /// <returns>An object that, when disposed, reenables change
        /// notifications.</returns>
        IDisposable SuppressChangeNotifications();
    }

    /// <summary>
    /// IReactiveNotifyPropertyChanged of TSender is a helper interface that adds
    /// typed versions of Changing and Changed.
    /// </summary>
    public interface IReactiveNotifyPropertyChanged<TSender> : IReactiveNotifyPropertyChanged
    {
        new IObservable<IObservedChange<TSender, object>> Changing { get; }
        new IObservable<IObservedChange<TSender, object>> Changed { get; }
    }

    /// <summary>
    /// This interface is implemented by RxUI objects which are given 
    /// IObservables as input - when the input IObservables OnError, instead of 
    /// disabling the RxUI object, we catch the IObservable and pipe it into
    /// this property.
    /// 
    /// Normally this IObservable is implemented with a ScheduledSubject whose 
    /// default Observer is RxApp.DefaultExceptionHandler - this means, that if
    /// you aren't listening to ThrownExceptions and one appears, the exception
    /// will appear on the UI thread and crash the application.
    /// </summary>
    public interface IHandleObservableErrors
    {
        /// <summary>
        /// Fires whenever an exception would normally terminate ReactiveUI 
        /// internal state.
        /// </summary>
        IObservable<Exception> ThrownExceptions { get; }
    }

    /// <summary>
    /// IReactiveCollection represents a collection that can notify when its
    /// contents are changed (either items are added/removed, or the object
    /// itself changes).
    ///
    /// It is important to implement the Changing/Changed from
    /// IReactiveNotifyPropertyChanged semantically as "Fire when *anything* in
    /// the collection or any of its items have changed, in any way".
    /// </summary>
    public interface IReactiveCollection : IReactiveNotifyPropertyChanged, IEnumerable, INotifyCollectionChanged
    {
        //
        // Collection Tracking
        //

        /// <summary>
        /// Fires when items are added to the collection, once per item added.
        /// Functions that add multiple items such AddRange should fire this
        /// multiple times. The object provided is the item that was added.
        /// </summary>
        IObservable<object> ItemsAdded { get; }

        /// <summary>
        /// Fires before an item is going to be added to the collection.
        /// </summary>
        IObservable<object> BeforeItemsAdded { get; }

        /// <summary>
        /// Fires once an item has been removed from a collection, providing the
        /// item that was removed.
        /// </summary>
        IObservable<object> ItemsRemoved { get; }

        /// <summary>
        /// Fires before an item will be removed from a collection, providing
        /// the item that will be removed. 
        /// </summary>
        IObservable<object> BeforeItemsRemoved { get; }

        /// <summary>
        /// Fires whenever the number of items in a collection has changed,
        /// providing the new Count.
        /// </summary>
        IObservable<int> CollectionCountChanged { get; }

        /// <summary>
        /// Fires before a collection is about to change, providing the previous
        /// Count.
        /// </summary>
        IObservable<int> CollectionCountChanging { get; }

        //
        // Change Tracking
        //

        /// <summary>
        /// Provides Item Changed notifications for any item in collection that
        /// implements IReactiveNotifyPropertyChanged. This is only enabled when
        /// ChangeTrackingEnabled is set to True.
        /// </summary>
        IObservable<IObservedChange<object, object>> ItemChanging { get; }

        /// <summary>
        /// Provides Item Changing notifications for any item in collection that
        /// implements IReactiveNotifyPropertyChanged. This is only enabled when
        /// </summary>
        IObservable<IObservedChange<object, object>> ItemChanged { get; }

        /// <summary>
        /// Enables the ItemChanging and ItemChanged properties; when this is
        /// enabled, whenever a property on any object implementing
        /// IReactiveNotifyPropertyChanged changes, the change will be
        /// rebroadcast through ItemChanging/ItemChanged.
        /// </summary>
        bool ChangeTrackingEnabled { get; set; }
    }

    /// <summary>
    /// IReactiveCollection of T is the typed version of IReactiveCollection and
    /// adds type-specified versions of Observables
    /// </summary>
    public interface IReactiveCollection<T> : IEnumerable<T>, IReactiveCollection
    {
        new IObservable<T> ItemsAdded { get; }

        new IObservable<T> BeforeItemsAdded { get; }

        new IObservable<T> ItemsRemoved { get; }

        new IObservable<T> BeforeItemsRemoved { get; }

        IObservable<IObservedChange<T, object>> ItemChanging { get; }

        IObservable<IObservedChange<T, object>> ItemChanged { get; }
    }

    /// <summary>
    /// IMessageBus represents an object that can act as a "Message Bus", a
    /// simple way for ViewModels and other objects to communicate with each
    /// other in a loosely coupled way.
    ///
    /// Specifying which messages go where is done via a combination of the Type
    /// of the message as well as an additional "Contract" parameter; this is a
    /// unique string used to distinguish between messages of the same Type, and
    /// is arbitrarily set by the client. 
    /// </summary>
    public interface IMessageBus : IEnableLogger
    {
        /// <summary>
        /// Registers a scheduler for the type, which may be specified at
        /// runtime, and the contract.
        /// </summary>
        /// <remarks>If a scheduler is already registered for the specified
        /// runtime and contract, this will overrwrite the existing
        /// registration.</remarks>
        /// <typeparam name="T">The type of the message to listen to.</typeparam>
        /// <param name="scheduler">The scheduler on which to post the
        /// notifications for the specified type and contract.
        /// RxApp.DeferredScheduler by default.</param>
        /// <param name="contract">A unique string to distinguish messages with
        /// identical types (i.e. "MyCoolViewModel") - if the message type is
        /// only used for one purpose, leave this as null.</param>
        void RegisterScheduler<T>(IScheduler scheduler, string contract = null);

        /// <summary>
        /// Listen provides an Observable that will fire whenever a Message is
        /// provided for this object via RegisterMessageSource or SendMessage.
        /// </summary>
        /// <typeparam name="T">The type of the message to listen to.</typeparam>
        /// <param name="contract">A unique string to distinguish messages with
        /// identical types (i.e. "MyCoolViewModel") - if the message type is
        /// only used for one purpose, leave this as null.</param>
        /// <returns></returns>
        IObservable<T> Listen<T>(string contract = null);

        /// <summary>
        /// Determines if a particular message Type is registered.
        /// </summary>
        /// <param name="type">The type of the message.</param>
        /// <param name="contract">A unique string to distinguish messages with
        /// identical types (i.e. "MyCoolViewModel") - if the message type is
        /// only used for one purpose, leave this as null.</param>
        /// <returns>True if messages have been posted for this message Type.</returns>
        bool IsRegistered(Type type, string contract = null);

        /// <summary>
        /// Registers an Observable representing the stream of messages to send.
        /// Another part of the code can then call Listen to retrieve this
        /// Observable.
        /// </summary>
        /// <typeparam name="T">The type of the message to listen to.</typeparam>
        /// <param name="source">An Observable that will be subscribed to, and a
        /// message sent out for each value provided.</param>
        /// <param name="contract">A unique string to distinguish messages with
        /// identical types (i.e. "MyCoolViewModel") - if the message type is
        /// only used for one purpose, leave this as null.</param>
        IDisposable RegisterMessageSource<T>(IObservable<T> source, string contract = null);

        /// <summary>
        /// Sends a single message using the specified Type and contract.
        /// Consider using RegisterMessageSource instead if you will be sending
        /// messages in response to other changes such as property changes
        /// or events.
        /// </summary>
        /// <typeparam name="T">The type of the message to send.</typeparam>
        /// <param name="message">The actual message to send</param>
        /// <param name="contract">A unique string to distinguish messages with
        /// identical types (i.e. "MyCoolViewModel") - if the message type is
        /// only used for one purpose, leave this as null.</param>
        void SendMessage<T>(T message, string contract = null);
    }
}

// vim: tw=120 ts=4 sw=4 et :
