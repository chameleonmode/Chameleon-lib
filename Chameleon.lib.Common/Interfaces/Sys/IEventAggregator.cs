using System.Globalization;
using System.Reflection;

namespace Chameleon.lib.Common.Interfaces.Sys;
public static class PubSubEventExtensions {
	public static void Publish<TPayload>(this PubSubEvent<TPayload> self)
			where TPayload : class, new()
	{
		self.Publish(new TPayload());
	}

	public static SubscriptionToken SubscribeOnUIThread<TPayload>(this PubSubEvent<TPayload> self,
			Action<TPayload> action, bool keepSubscriberReferenceAlive = true)
	{
		return self.Subscribe(action, ThreadOption.UIThread, keepSubscriberReferenceAlive);
	}

	public static SubscriptionToken SubscribeOnBackgroundThread<TPayload>(this PubSubEvent<TPayload> self,
			Action<TPayload> action, bool keepSubscriberReferenceAlive = true)
	{
		return self.Subscribe(action, ThreadOption.BackgroundThread, keepSubscriberReferenceAlive);
	}

	public static IDisposable SubscribeOnce<TPayload>(this PubSubEvent<TPayload> self,
			Action<TPayload> action,
			ThreadOption thread = ThreadOption.PublisherThread,
			bool keepSubscriberReferenceAlive = true)
	{
		var subscription = new PubSubSubscription();
		var subscriptionToken = self.Subscribe((payload) => {
			if (subscription.HasToken) {
				subscription.Dispose();
				action(payload);
			}
		}, thread, keepSubscriberReferenceAlive);
		subscription.Token = subscriptionToken;
		return subscription;
	}
	public static IDisposable SubscribeOnce<TPayload>(this PubSubEvent<TPayload> self,
				 Action<TPayload> action, bool keepSubscriberReferenceAlive = true)
	{
		return self.SubscribeOnce(action, ThreadOption.PublisherThread, keepSubscriberReferenceAlive);
	}

	public static IDisposable SubscribeOnce<TPayload>(this PubSubEvent<TPayload> self,
				 Action action,
				 bool keepSubscriberReferenceAlive = true)
	{
		return self.SubscribeOnce(_ => action(),
				keepSubscriberReferenceAlive);
	}

	public static IDisposable SubscribeOnce(this PubSubEvent self,
			Action action,
			ThreadOption thread = ThreadOption.PublisherThread,
			bool keepSubscriberReferenceAlive = true)
	{
		var subscription = new PubSubSubscription();
		var subscriptionToken = self.Subscribe(() => {
			if (subscription.HasToken) {
				subscription.Dispose();
				action();
			}
		}, thread, keepSubscriberReferenceAlive);
		subscription.Token = subscriptionToken;
		return subscription;
	}




	private class PubSubSubscription : IDisposable {
		public SubscriptionToken Token { get; set; }
		public bool HasToken => Token != null;

		public void Dispose()
		{
			Token?.Dispose();
			Token = null;
		}
	}
}

//
// Summary:
//     Specifies on which thread a Prism.Events.PubSubEvent`1 subscriber will be called.
public enum ThreadOption {
	//
	// Summary:
	//     The call is done on the same thread on which the Prism.Events.PubSubEvent`1 was
	//     published.
	PublisherThread,
	//
	// Summary:
	//     The call is done on the UI thread.
	UIThread,
	//
	// Summary:
	//     The call is done asynchronously on a background thread.
	BackgroundThread
}

/// <summary>
/// Represents a reference to a <see cref="Delegate"/> that may contain a
/// <see cref="WeakReference"/> to the target. This class is used
/// internally by the Prism Library.
/// </summary>
public class DelegateReference : IDelegateReference {
	private readonly Delegate _delegate;
	private readonly WeakReference _weakReference;
	private readonly MethodInfo _method;
	private readonly Type _delegateType;

	/// <summary>
	/// Initializes a new instance of <see cref="DelegateReference"/>.
	/// </summary>
	/// <param name="delegate">The original <see cref="Delegate"/> to create a reference for.</param>
	/// <param name="keepReferenceAlive">If <see langword="false" /> the class will create a weak reference to the delegate, allowing it to be garbage collected. Otherwise it will keep a strong reference to the target.</param>
	/// <exception cref="ArgumentNullException">If the passed <paramref name="delegate"/> is not assignable to <see cref="Delegate"/>.</exception>
	public DelegateReference(Delegate @delegate, bool keepReferenceAlive)
	{
		if (@delegate == null)
			throw new ArgumentNullException("delegate");

		if (keepReferenceAlive) _delegate = @delegate;
		else {
			_weakReference = new WeakReference(@delegate.Target);
			_method = @delegate.GetMethodInfo();
			_delegateType = @delegate.GetType();
		}
	}

	/// <summary>
	/// Gets the <see cref="Delegate" /> (the target) referenced by the current <see cref="DelegateReference"/> object.
	/// </summary>
	/// <value><see langword="null"/> if the object referenced by the current <see cref="DelegateReference"/> object has been garbage collected; otherwise, a reference to the <see cref="Delegate"/> referenced by the current <see cref="DelegateReference"/> object.</value>
	public Delegate Target {
		get {
			if (_delegate != null) return _delegate;
			else {
				return TryGetDelegate();
			}
		}
	}

	/// <summary>
	/// Checks if the <see cref="Delegate" /> (the target) referenced by the current <see cref="DelegateReference"/> object are equal to another <see cref="Delegate" />.
	/// This is equivalent with comparing <see cref="Target"/> with <paramref name="delegate"/>, only more efficient.
	/// </summary>
	/// <param name="delegate">The other delegate to compare with.</param>
	/// <returns>True if the target referenced by the current object are equal to <paramref name="delegate"/>.</returns>
	public bool TargetEquals(Delegate @delegate)
	{
		if (_delegate != null) return _delegate == @delegate;
		if (@delegate == null) return !_method.IsStatic && !_weakReference.IsAlive;
		return _weakReference.Target == @delegate.Target && Equals(_method, @delegate.GetMethodInfo());
	}

	private Delegate TryGetDelegate()
	{
		if (_method.IsStatic) return _method.CreateDelegate(_delegateType, null);
		var target = _weakReference.Target;
		if (target != null) return _method.CreateDelegate(_delegateType, target);
		return null;
	}
}

//
// Summary:
//     Represents a reference to a System.Delegate.
public interface IDelegateReference {
	//
	// Summary:
	//     Gets the referenced System.Delegate object.
	//
	// Value:
	//     A System.Delegate instance if the target is valid; otherwise null.
	Delegate Target { get; }
}

/// <summary>
/// Provides a way to retrieve a <see cref="Delegate"/> to execute an action depending
/// on the value of a second filter predicate that returns true if the action should execute.
/// </summary>
public class EventSubscription : IEventSubscription {
	private readonly IDelegateReference _actionReference;

	///<summary>
	/// Creates a new instance of <see cref="EventSubscription"/>.
	///</summary>
	///<param name="actionReference">A reference to a delegate of type <see cref="System.Action"/>.</param>
	///<exception cref="ArgumentNullException">When <paramref name="actionReference"/> or <see paramref="filterReference"/> are <see langword="null" />.</exception>
	///<exception cref="ArgumentException">When the target of <paramref name="actionReference"/> is not of type <see cref="System.Action"/>.</exception>
	public EventSubscription(IDelegateReference actionReference)
	{
		if (actionReference == null)
			throw new ArgumentNullException(nameof(actionReference));
		if (!(actionReference.Target is Action))
			throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "InvalidDelegateRerefenceTypeException", typeof(Action).FullName), nameof(actionReference));

		_actionReference = actionReference;
	}

	/// <summary>
	/// Gets the target <see cref="System.Action"/> that is referenced by the <see cref="IDelegateReference"/>.
	/// </summary>
	/// <value>An <see cref="System.Action"/> or <see langword="null" /> if the referenced target is not alive.</value>
	public Action Action {
		get { return (Action)_actionReference.Target; }
	}

	/// <summary>
	/// Gets or sets a <see cref="SubscriptionToken"/> that identifies this <see cref="IEventSubscription"/>.
	/// </summary>
	/// <value>A token that identifies this <see cref="IEventSubscription"/>.</value>
	public SubscriptionToken SubscriptionToken { get; set; }

	/// <summary>
	/// Gets the execution strategy to publish this event.
	/// </summary>
	/// <returns>An <see cref="System.Action"/> with the execution strategy, or <see langword="null" /> if the <see cref="IEventSubscription"/> is no longer valid.</returns>
	/// <remarks>
	/// If <see cref="Action"/>is no longer valid because it was
	/// garbage collected, this method will return <see langword="null" />.
	/// Otherwise it will return a delegate that evaluates the <see cref="EventSubscription{TPayload}.Filter"/> and if it
	/// returns <see langword="true" /> will then call <see cref="InvokeAction"/>. The returned
	/// delegate holds a hard reference to the <see cref="Action"/> target
	/// <see cref="Delegate">delegates</see>. As long as the returned delegate is not garbage collected,
	/// the <see cref="Action"/> references delegates won't get collected either.
	/// </remarks>
	public virtual Action<object[]> GetExecutionStrategy()
	{
		var action = Action;
		if (action != null) {
			return arguments => {
				InvokeAction(action);
			};
		}
		return null;
	}

	/// <summary>
	/// Invokes the specified <see cref="Action{TPayload}"/> synchronously when not overridden.
	/// </summary>
	/// <param name="action">The action to execute.</param>
	/// <exception cref="ArgumentNullException">An <see cref="ArgumentNullException"/> is thrown if <paramref name="action"/> is null.</exception>
	public virtual void InvokeAction(Action action)
	{
		if (action == null) throw new ArgumentNullException(nameof(action));

		action();
	}
}

/// <summary>
/// Provides a way to retrieve a <see cref="Delegate"/> to execute an action depending
/// on the value of a second filter predicate that returns true if the action should execute.
/// </summary>
/// <typeparam name="TPayload">The type to use for the generic <see cref="Action{TPayload}"/> and <see cref="Predicate{TPayload}"/> types.</typeparam>
public class EventSubscription<TPayload> : IEventSubscription {
	private readonly IDelegateReference _actionReference;
	private readonly IDelegateReference _filterReference;

	///<summary>
	/// Creates a new instance of <see cref="EventSubscription{TPayload}"/>.
	///</summary>
	///<param name="actionReference">A reference to a delegate of type <see cref="Action{TPayload}"/>.</param>
	///<param name="filterReference">A reference to a delegate of type <see cref="Predicate{TPayload}"/>.</param>
	///<exception cref="ArgumentNullException">When <paramref name="actionReference"/> or <see paramref="filterReference"/> are <see langword="null" />.</exception>
	///<exception cref="ArgumentException">When the target of <paramref name="actionReference"/> is not of type <see cref="Action{TPayload}"/>,
	///or the target of <paramref name="filterReference"/> is not of type <see cref="Predicate{TPayload}"/>.</exception>
	public EventSubscription(IDelegateReference actionReference, IDelegateReference filterReference)
	{
		if (actionReference == null)
			throw new ArgumentNullException(nameof(actionReference));
		if (!(actionReference.Target is Action<TPayload>))
			throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "Resources.InvalidDelegateRerefenceTypeException", typeof(Action<TPayload>).FullName), nameof(actionReference));

		if (filterReference == null)
			throw new ArgumentNullException(nameof(filterReference));
		if (!(filterReference.Target is Predicate<TPayload>))
			throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "Resources.InvalidDelegateRerefenceTypeException", typeof(Predicate<TPayload>).FullName), nameof(filterReference));

		_actionReference = actionReference;
		_filterReference = filterReference;
	}

	/// <summary>
	/// Gets the target <see cref="Action{T}"/> that is referenced by the <see cref="IDelegateReference"/>.
	/// </summary>
	/// <value>An <see cref="Action{T}"/> or <see langword="null" /> if the referenced target is not alive.</value>
	public Action<TPayload> Action {
		get { return (Action<TPayload>)_actionReference.Target; }
	}

	/// <summary>
	/// Gets the target <see cref="Predicate{T}"/> that is referenced by the <see cref="IDelegateReference"/>.
	/// </summary>
	/// <value>An <see cref="Predicate{T}"/> or <see langword="null" /> if the referenced target is not alive.</value>
	public Predicate<TPayload> Filter {
		get { return (Predicate<TPayload>)_filterReference.Target; }
	}

	/// <summary>
	/// Gets or sets a <see cref="SubscriptionToken"/> that identifies this <see cref="IEventSubscription"/>.
	/// </summary>
	/// <value>A token that identifies this <see cref="IEventSubscription"/>.</value>
	public SubscriptionToken SubscriptionToken { get; set; }

	/// <summary>
	/// Gets the execution strategy to publish this event.
	/// </summary>
	/// <returns>An <see cref="Action{T}"/> with the execution strategy, or <see langword="null" /> if the <see cref="IEventSubscription"/> is no longer valid.</returns>
	/// <remarks>
	/// If <see cref="Action"/> or <see cref="Filter"/> are no longer valid because they were
	/// garbage collected, this method will return <see langword="null" />.
	/// Otherwise it will return a delegate that evaluates the <see cref="Filter"/> and if it
	/// returns <see langword="true" /> will then call <see cref="InvokeAction"/>. The returned
	/// delegate holds hard references to the <see cref="Action"/> and <see cref="Filter"/> target
	/// <see cref="Delegate">delegates</see>. As long as the returned delegate is not garbage collected,
	/// the <see cref="Action"/> and <see cref="Filter"/> references delegates won't get collected either.
	/// </remarks>
	public virtual Action<object[]> GetExecutionStrategy()
	{
		var action = Action;
		var filter = Filter;
		if (action != null && filter != null) {
			return arguments => {
				var argument = default(TPayload);
				if (arguments != null && arguments.Length > 0 && arguments[0] != null) argument = (TPayload)arguments[0];
				if (filter(argument)) InvokeAction(action, argument);
			};
		}
		return null;
	}

	/// <summary>
	/// Invokes the specified <see cref="Action{TPayload}"/> synchronously when not overridden.
	/// </summary>
	/// <param name="action">The action to execute.</param>
	/// <param name="argument">The payload to pass <paramref name="action"/> while invoking it.</param>
	/// <exception cref="ArgumentNullException">An <see cref="ArgumentNullException"/> is thrown if <paramref name="action"/> is null.</exception>
	public virtual void InvokeAction(Action<TPayload> action, TPayload argument)
	{
		if (action == null) throw new ArgumentNullException(nameof(action));

		action(argument);
	}
}

/// <summary>
/// Extends <see cref="EventSubscription"/> to invoke the <see cref="EventSubscription.Action"/> delegate in a background thread.
/// </summary>
public class BackgroundEventSubscription : EventSubscription {
	/// <summary>
	/// Creates a new instance of <see cref="BackgroundEventSubscription"/>.
	/// </summary>
	/// <param name="actionReference">A reference to a delegate of type <see cref="Action"/>.</param>
	/// <exception cref="ArgumentNullException">When <paramref name="actionReference"/> or <see paramref="filterReference"/> are <see langword="null" />.</exception>
	/// <exception cref="ArgumentException">When the target of <paramref name="actionReference"/> is not of type <see cref="Action"/>.</exception>
	public BackgroundEventSubscription(IDelegateReference actionReference)
			: base(actionReference)
	{
	}

	/// <summary>
	/// Invokes the specified <see cref="Action"/> in an asynchronous thread by using a <see cref="Task"/>.
	/// </summary>
	/// <param name="action">The action to execute.</param>
	public override void InvokeAction(Action action)
	{
		Task.Run(action);
	}
}

/// <summary>
/// Extends <see cref="EventSubscription{TPayload}"/> to invoke the <see cref="EventSubscription{TPayload}.Action"/> delegate in a background thread.
/// </summary>
/// <typeparam name="TPayload">The type to use for the generic <see cref="Action{TPayload}"/> and <see cref="Predicate{TPayload}"/> types.</typeparam>
public class BackgroundEventSubscription<TPayload> : EventSubscription<TPayload> {
	/// <summary>
	/// Creates a new instance of <see cref="BackgroundEventSubscription{TPayload}"/>.
	/// </summary>
	/// <param name="actionReference">A reference to a delegate of type <see cref="Action{TPayload}"/>.</param>
	/// <param name="filterReference">A reference to a delegate of type <see cref="Predicate{TPayload}"/>.</param>
	/// <exception cref="ArgumentNullException">When <paramref name="actionReference"/> or <see paramref="filterReference"/> are <see langword="null" />.</exception>
	/// <exception cref="ArgumentException">When the target of <paramref name="actionReference"/> is not of type <see cref="Action{TPayload}"/>,
	/// or the target of <paramref name="filterReference"/> is not of type <see cref="Predicate{TPayload}"/>.</exception>
	public BackgroundEventSubscription(IDelegateReference actionReference, IDelegateReference filterReference)
			: base(actionReference, filterReference)
	{
	}

	/// <summary>
	/// Invokes the specified <see cref="Action{TPayload}"/> in an asynchronous thread by using a <see cref="ThreadPool"/>.
	/// </summary>
	/// <param name="action">The action to execute.</param>
	/// <param name="argument">The payload to pass <paramref name="action"/> while invoking it.</param>
	public override void InvokeAction(Action<TPayload> action, TPayload argument)
	{
		//ThreadPool.QueueUserWorkItem( (o) => action(argument) );
		Task.Run(() => action(argument));
	}
}

///<summary>
/// Extends <see cref="EventSubscription"/> to invoke the <see cref="EventSubscription.Action"/> delegate
/// in a specific <see cref="SynchronizationContext"/>.
///</summary>
public class DispatcherEventSubscription : EventSubscription {
	private readonly SynchronizationContext syncContext;

	///<summary>
	/// Creates a new instance of <see cref="BackgroundEventSubscription"/>.
	///</summary>
	///<param name="actionReference">A reference to a delegate of type <see cref="Action{TPayload}"/>.</param>
	///<param name="context">The synchronization context to use for UI thread dispatching.</param>
	///<exception cref="ArgumentNullException">When <paramref name="actionReference"/> or <see paramref="filterReference"/> are <see langword="null" />.</exception>
	///<exception cref="ArgumentException">When the target of <paramref name="actionReference"/> is not of type <see cref="Action{TPayload}"/>.</exception>
	public DispatcherEventSubscription(IDelegateReference actionReference, SynchronizationContext context)
			: base(actionReference)
	{
		syncContext = context;
	}

	/// <summary>
	/// Invokes the specified <see cref="Action{TPayload}"/> asynchronously in the specified <see cref="SynchronizationContext"/>.
	/// </summary>
	/// <param name="action">The action to execute.</param>
	public override void InvokeAction(Action action)
	{
		syncContext.Post((o) => action(), null);
	}
}

///<summary>
/// Extends <see cref="EventSubscription{TPayload}"/> to invoke the <see cref="EventSubscription{TPayload}.Action"/> delegate
/// in a specific <see cref="SynchronizationContext"/>.
///</summary>
/// <typeparam name="TPayload">The type to use for the generic <see cref="Action{TPayload}"/> and <see cref="Predicate{TPayload}"/> types.</typeparam>
public class DispatcherEventSubscription<TPayload> : EventSubscription<TPayload> {
	private readonly SynchronizationContext syncContext;

	///<summary>
	/// Creates a new instance of <see cref="BackgroundEventSubscription{TPayload}"/>.
	///</summary>
	///<param name="actionReference">A reference to a delegate of type <see cref="Action{TPayload}"/>.</param>
	///<param name="filterReference">A reference to a delegate of type <see cref="Predicate{TPayload}"/>.</param>
	///<param name="context">The synchronization context to use for UI thread dispatching.</param>
	///<exception cref="ArgumentNullException">When <paramref name="actionReference"/> or <see paramref="filterReference"/> are <see langword="null" />.</exception>
	///<exception cref="ArgumentException">When the target of <paramref name="actionReference"/> is not of type <see cref="Action{TPayload}"/>,
	///or the target of <paramref name="filterReference"/> is not of type <see cref="Predicate{TPayload}"/>.</exception>
	public DispatcherEventSubscription(IDelegateReference actionReference, IDelegateReference filterReference, SynchronizationContext context)
			: base(actionReference, filterReference)
	{
		syncContext = context;
	}

	/// <summary>
	/// Invokes the specified <see cref="Action{TPayload}"/> asynchronously in the specified <see cref="SynchronizationContext"/>.
	/// </summary>
	/// <param name="action">The action to execute.</param>
	/// <param name="argument">The payload to pass <paramref name="action"/> while invoking it.</param>
	public override void InvokeAction(Action<TPayload> action, TPayload argument)
	{
		syncContext.Post((o) => action((TPayload)o), argument);
	}
}

/// <summary>
/// Defines a class that manages publication and subscription to events.
/// </summary>
public class PubSubEvent : EventBase {
	/// <summary>
	/// Subscribes a delegate to an event that will be published on the <see cref="ThreadOption.PublisherThread"/>.
	/// <see cref="PubSubEvent"/> will maintain a <see cref="WeakReference"/> to the target of the supplied <paramref name="action"/> delegate.
	/// </summary>
	/// <param name="action">The delegate that gets executed when the event is published.</param>
	/// <returns>A <see cref="SubscriptionToken"/> that uniquely identifies the added subscription.</returns>
	/// <remarks>
	/// The PubSubEvent collection is thread-safe.
	/// </remarks>
	public SubscriptionToken Subscribe(Action action)
	{
		return Subscribe(action, ThreadOption.PublisherThread);
	}

	/// <summary>
	/// Subscribes a delegate to an event.
	/// PubSubEvent will maintain a <see cref="WeakReference"/> to the Target of the supplied <paramref name="action"/> delegate.
	/// </summary>
	/// <param name="action">The delegate that gets executed when the event is raised.</param>
	/// <param name="threadOption">Specifies on which thread to receive the delegate callback.</param>
	/// <returns>A <see cref="SubscriptionToken"/> that uniquely identifies the added subscription.</returns>
	/// <remarks>
	/// The PubSubEvent collection is thread-safe.
	/// </remarks>
	public SubscriptionToken Subscribe(Action action, ThreadOption threadOption)
	{
		return Subscribe(action, threadOption, false);
	}

	/// <summary>
	/// Subscribes a delegate to an event that will be published on the <see cref="ThreadOption.PublisherThread"/>.
	/// </summary>
	/// <param name="action">The delegate that gets executed when the event is published.</param>
	/// <param name="keepSubscriberReferenceAlive">When <see langword="true"/>, the <see cref="PubSubEvent"/> keeps a reference to the subscriber so it does not get garbage collected.</param>
	/// <returns>A <see cref="SubscriptionToken"/> that uniquely identifies the added subscription.</returns>
	/// <remarks>
	/// If <paramref name="keepSubscriberReferenceAlive"/> is set to <see langword="false" />, <see cref="PubSubEvent"/> will maintain a <see cref="WeakReference"/> to the Target of the supplied <paramref name="action"/> delegate.
	/// If not using a WeakReference (<paramref name="keepSubscriberReferenceAlive"/> is <see langword="true" />), the user must explicitly call Unsubscribe for the event when disposing the subscriber in order to avoid memory leaks or unexpected behavior.
	/// <para/>
	/// The PubSubEvent collection is thread-safe.
	/// </remarks>
	public SubscriptionToken Subscribe(Action action, bool keepSubscriberReferenceAlive)
	{
		return Subscribe(action, ThreadOption.PublisherThread, keepSubscriberReferenceAlive);
	}

	/// <summary>
	/// Subscribes a delegate to an event.
	/// </summary>
	/// <param name="action">The delegate that gets executed when the event is published.</param>
	/// <param name="threadOption">Specifies on which thread to receive the delegate callback.</param>
	/// <param name="keepSubscriberReferenceAlive">When <see langword="true"/>, the <see cref="PubSubEvent"/> keeps a reference to the subscriber so it does not get garbage collected.</param>
	/// <returns>A <see cref="SubscriptionToken"/> that uniquely identifies the added subscription.</returns>
	/// <remarks>
	/// If <paramref name="keepSubscriberReferenceAlive"/> is set to <see langword="false" />, <see cref="PubSubEvent"/> will maintain a <see cref="WeakReference"/> to the Target of the supplied <paramref name="action"/> delegate.
	/// If not using a WeakReference (<paramref name="keepSubscriberReferenceAlive"/> is <see langword="true" />), the user must explicitly call Unsubscribe for the event when disposing the subscriber in order to avoid memory leaks or unexpected behavior.
	/// <para/>
	/// The PubSubEvent collection is thread-safe.
	/// </remarks>
	public virtual SubscriptionToken Subscribe(Action action, ThreadOption threadOption, bool keepSubscriberReferenceAlive)
	{
		IDelegateReference actionReference = new DelegateReference(action, keepSubscriberReferenceAlive);

		EventSubscription subscription;
		switch (threadOption) {
			case ThreadOption.PublisherThread:
				subscription = new EventSubscription(actionReference);
				break;
			case ThreadOption.BackgroundThread:
				subscription = new BackgroundEventSubscription(actionReference);
				break;
			case ThreadOption.UIThread:
				if (SynchronizationContext == null) throw new InvalidOperationException("Resources.EventAggregatorNotConstructedOnUIThread");
				subscription = new DispatcherEventSubscription(actionReference, SynchronizationContext);
				break;
			default:
				subscription = new EventSubscription(actionReference);
				break;
		}

		return InternalSubscribe(subscription);
	}

	/// <summary>
	/// Publishes the <see cref="PubSubEvent"/>.
	/// </summary>
	public virtual void Publish()
	{
		InternalPublish();
	}

	/// <summary>
	/// Removes the first subscriber matching <see cref="Action"/> from the subscribers' list.
	/// </summary>
	/// <param name="subscriber">The <see cref="Action"/> used when subscribing to the event.</param>
	public virtual void Unsubscribe(Action subscriber)
	{
		lock (Subscriptions) {
			IEventSubscription eventSubscription = Subscriptions.Cast<EventSubscription>().FirstOrDefault(evt => evt.Action == subscriber);
			if (eventSubscription != null) Subscriptions.Remove(eventSubscription);
		}
	}

	/// <summary>
	/// Returns <see langword="true"/> if there is a subscriber matching <see cref="Action"/>.
	/// </summary>
	/// <param name="subscriber">The <see cref="Action"/> used when subscribing to the event.</param>
	/// <returns><see langword="true"/> if there is an <see cref="Action"/> that matches; otherwise <see langword="false"/>.</returns>
	public virtual bool Contains(Action subscriber)
	{
		IEventSubscription eventSubscription;
		lock (Subscriptions) {
			eventSubscription = Subscriptions.Cast<EventSubscription>().FirstOrDefault(evt => evt.Action == subscriber);
		}
		return eventSubscription != null;
	}
}

/// <summary>
/// Defines a class that manages publication and subscription to events.
/// </summary>
/// <typeparam name="TPayload">The type of message that will be passed to the subscribers.</typeparam>
public class PubSubEvent<TPayload> : EventBase {
	/// <summary>
	/// Subscribes a delegate to an event that will be published on the <see cref="ThreadOption.PublisherThread"/>.
	/// <see cref="PubSubEvent{TPayload}"/> will maintain a <see cref="WeakReference"/> to the target of the supplied <paramref name="action"/> delegate.
	/// </summary>
	/// <param name="action">The delegate that gets executed when the event is published.</param>
	/// <returns>A <see cref="SubscriptionToken"/> that uniquely identifies the added subscription.</returns>
	/// <remarks>
	/// The PubSubEvent collection is thread-safe.
	/// </remarks>
	public SubscriptionToken Subscribe(Action<TPayload> action)
	{
		return Subscribe(action, ThreadOption.PublisherThread);
	}

	/// <summary>
	/// Subscribes a delegate to an event that will be published on the <see cref="ThreadOption.PublisherThread"/>
	/// </summary>
	/// <param name="action">The delegate that gets executed when the event is raised.</param>
	/// <param name="filter">Filter to evaluate if the subscriber should receive the event.</param>
	/// <returns>A <see cref="SubscriptionToken"/> that uniquely identifies the added subscription.</returns>
	public virtual SubscriptionToken Subscribe(Action<TPayload> action, Predicate<TPayload> filter)
	{
		return Subscribe(action, ThreadOption.PublisherThread, false, filter);
	}

	/// <summary>
	/// Subscribes a delegate to an event.
	/// PubSubEvent will maintain a <see cref="WeakReference"/> to the Target of the supplied <paramref name="action"/> delegate.
	/// </summary>
	/// <param name="action">The delegate that gets executed when the event is raised.</param>
	/// <param name="threadOption">Specifies on which thread to receive the delegate callback.</param>
	/// <returns>A <see cref="SubscriptionToken"/> that uniquely identifies the added subscription.</returns>
	/// <remarks>
	/// The PubSubEvent collection is thread-safe.
	/// </remarks>
	public SubscriptionToken Subscribe(Action<TPayload> action, ThreadOption threadOption)
	{
		return Subscribe(action, threadOption, false);
	}

	/// <summary>
	/// Subscribes a delegate to an event that will be published on the <see cref="ThreadOption.PublisherThread"/>.
	/// </summary>
	/// <param name="action">The delegate that gets executed when the event is published.</param>
	/// <param name="keepSubscriberReferenceAlive">When <see langword="true"/>, the <see cref="PubSubEvent{TPayload}"/> keeps a reference to the subscriber so it does not get garbage collected.</param>
	/// <returns>A <see cref="SubscriptionToken"/> that uniquely identifies the added subscription.</returns>
	/// <remarks>
	/// If <paramref name="keepSubscriberReferenceAlive"/> is set to <see langword="false" />, <see cref="PubSubEvent{TPayload}"/> will maintain a <see cref="WeakReference"/> to the Target of the supplied <paramref name="action"/> delegate.
	/// If not using a WeakReference (<paramref name="keepSubscriberReferenceAlive"/> is <see langword="true" />), the user must explicitly call Unsubscribe for the event when disposing the subscriber in order to avoid memory leaks or unexpected behavior.
	/// <para/>
	/// The PubSubEvent collection is thread-safe.
	/// </remarks>
	public SubscriptionToken Subscribe(Action<TPayload> action, bool keepSubscriberReferenceAlive)
	{
		return Subscribe(action, ThreadOption.PublisherThread, keepSubscriberReferenceAlive);
	}

	/// <summary>
	/// Subscribes a delegate to an event.
	/// </summary>
	/// <param name="action">The delegate that gets executed when the event is published.</param>
	/// <param name="threadOption">Specifies on which thread to receive the delegate callback.</param>
	/// <param name="keepSubscriberReferenceAlive">When <see langword="true"/>, the <see cref="PubSubEvent{TPayload}"/> keeps a reference to the subscriber so it does not get garbage collected.</param>
	/// <returns>A <see cref="SubscriptionToken"/> that uniquely identifies the added subscription.</returns>
	/// <remarks>
	/// If <paramref name="keepSubscriberReferenceAlive"/> is set to <see langword="false" />, <see cref="PubSubEvent{TPayload}"/> will maintain a <see cref="WeakReference"/> to the Target of the supplied <paramref name="action"/> delegate.
	/// If not using a WeakReference (<paramref name="keepSubscriberReferenceAlive"/> is <see langword="true" />), the user must explicitly call Unsubscribe for the event when disposing the subscriber in order to avoid memory leaks or unexpected behavior.
	/// <para/>
	/// The PubSubEvent collection is thread-safe.
	/// </remarks>
	public SubscriptionToken Subscribe(Action<TPayload> action, ThreadOption threadOption, bool keepSubscriberReferenceAlive)
	{
		return Subscribe(action, threadOption, keepSubscriberReferenceAlive, null);
	}

	/// <summary>
	/// Subscribes a delegate to an event.
	/// </summary>
	/// <param name="action">The delegate that gets executed when the event is published.</param>
	/// <param name="threadOption">Specifies on which thread to receive the delegate callback.</param>
	/// <param name="keepSubscriberReferenceAlive">When <see langword="true"/>, the <see cref="PubSubEvent{TPayload}"/> keeps a reference to the subscriber so it does not get garbage collected.</param>
	/// <param name="filter">Filter to evaluate if the subscriber should receive the event.</param>
	/// <returns>A <see cref="SubscriptionToken"/> that uniquely identifies the added subscription.</returns>
	/// <remarks>
	/// If <paramref name="keepSubscriberReferenceAlive"/> is set to <see langword="false" />, <see cref="PubSubEvent{TPayload}"/> will maintain a <see cref="WeakReference"/> to the Target of the supplied <paramref name="action"/> delegate.
	/// If not using a WeakReference (<paramref name="keepSubscriberReferenceAlive"/> is <see langword="true" />), the user must explicitly call Unsubscribe for the event when disposing the subscriber in order to avoid memory leaks or unexpected behavior.
	///
	/// The PubSubEvent collection is thread-safe.
	/// </remarks>
	public virtual SubscriptionToken Subscribe(Action<TPayload> action, ThreadOption threadOption, bool keepSubscriberReferenceAlive, Predicate<TPayload> filter)
	{
		IDelegateReference actionReference = new DelegateReference(action, keepSubscriberReferenceAlive);
		IDelegateReference filterReference;
		if (filter != null) filterReference = new DelegateReference(filter, keepSubscriberReferenceAlive);
		else {
			filterReference = new DelegateReference(new Predicate<TPayload>(delegate { return true; }), true);
		}
		EventSubscription<TPayload> subscription;
		switch (threadOption) {
			case ThreadOption.PublisherThread:
				subscription = new EventSubscription<TPayload>(actionReference, filterReference);
				break;
			case ThreadOption.BackgroundThread:
				subscription = new BackgroundEventSubscription<TPayload>(actionReference, filterReference);
				break;
			case ThreadOption.UIThread:
				if (SynchronizationContext == null) throw new InvalidOperationException("Resources.EventAggregatorNotConstructedOnUIThread");
				subscription = new DispatcherEventSubscription<TPayload>(actionReference, filterReference, SynchronizationContext);
				break;
			default:
				subscription = new EventSubscription<TPayload>(actionReference, filterReference);
				break;
		}

		return InternalSubscribe(subscription);
	}

	/// <summary>
	/// Publishes the <see cref="PubSubEvent{TPayload}"/>.
	/// </summary>
	/// <param name="payload">Message to pass to the subscribers.</param>
	public virtual void Publish(TPayload payload)
	{
		InternalPublish(payload);
	}

	/// <summary>
	/// Removes the first subscriber matching <see cref="Action{TPayload}"/> from the subscribers' list.
	/// </summary>
	/// <param name="subscriber">The <see cref="Action{TPayload}"/> used when subscribing to the event.</param>
	public virtual void Unsubscribe(Action<TPayload> subscriber)
	{
		lock (Subscriptions) {
			IEventSubscription eventSubscription = Subscriptions.Cast<EventSubscription<TPayload>>().FirstOrDefault(evt => evt.Action == subscriber);
			if (eventSubscription != null) Subscriptions.Remove(eventSubscription);
		}
	}

	/// <summary>
	/// Returns <see langword="true"/> if there is a subscriber matching <see cref="Action{TPayload}"/>.
	/// </summary>
	/// <param name="subscriber">The <see cref="Action{TPayload}"/> used when subscribing to the event.</param>
	/// <returns><see langword="true"/> if there is an <see cref="Action{TPayload}"/> that matches; otherwise <see langword="false"/>.</returns>
	public virtual bool Contains(Action<TPayload> subscriber)
	{
		IEventSubscription eventSubscription;
		lock (Subscriptions) {
			eventSubscription = Subscriptions.Cast<EventSubscription<TPayload>>().FirstOrDefault(evt => evt.Action == subscriber);
		}
		return eventSubscription != null;
	}
}

//
// Summary:
//     Defines an interface to get instances of an event type.
public interface IEventAggregator {
	//
	// Summary:
	//     Gets an instance of an event type.
	//
	// Type parameters:
	//   TEventType:
	//     The type of event to get.
	//
	// Returns:
	//     An instance of an event object of type TEventType.
	TEventType GetEvent<TEventType>()
			where TEventType : EventBase, new();

	void Pub<TEventType>(params object[] args)
			where TEventType : EventBase, new();

	SubscriptionToken Sub<TEventType, TPayload>(Action<TPayload> action)
			where TEventType : PubSubEvent<TPayload>, new();

	void Sub<TEventType>(Action subscription)
			where TEventType : PubSubEvent, new();

	void Push<TEventType, TPayload, T>(T param)
			where TEventType : PubSubEvent<TPayload>, new()
			where TPayload : EventArgs;
	void Push<TEventType, TPayload>(params object[] p)
			where TEventType : PubSubEvent<TPayload>, new()
			where TPayload : EventArgs;
	void Push<TEventType, TPayload>(TPayload load)
			where TEventType : PubSubEvent<TPayload>, new();
}

/// <summary>
/// Implements <see cref="IEventAggregator"/>.
/// </summary>
public class EventAggregator : IEventAggregator {
	private static IEventAggregator? _current;

	/// <summary>
	/// Gets or Sets the Current Instance of the <see cref="IEventAggregator"/>
	/// </summary>
	public static IEventAggregator Current {
		get => _current ??= new EventAggregator();
		set => _current = value;
	}

	public void Pub<TEventType>(params object[] args) where TEventType : EventBase, new()
			=> GetEvent<TEventType>().InternalPublish(args);

	public void Sub<TEventType>(Action subscription) where TEventType : PubSubEvent, new()
			=> GetEvent<TEventType>().Subscribe(subscription);

	public SubscriptionToken Sub<TEventType, TPayload>(Action<TPayload> action)
			where TEventType : PubSubEvent<TPayload>, new()
	{
		return GetEvent<TEventType>().Subscribe(action);
	}


	/// <summary>
	/// Creates a new instance of the <see cref="EventAggregator"/>
	/// </summary>
	public EventAggregator()
	{
		if (_current is null) _current = this;
	}

	private readonly Dictionary<Type, EventBase> events = [];
	// Captures the sync context for the UI thread when constructed on the UI thread
	// in a platform agnostic way so it can be used for UI thread dispatching
	private readonly SynchronizationContext syncContext = SynchronizationContext.Current;

	/// <summary>
	/// Gets the single instance of the event managed by this EventAggregator. Multiple calls to this method with the same <typeparamref name="TEventType"/> returns the same event instance.
	/// </summary>
	/// <typeparam name="TEventType">The type of event to get. This must inherit from <see cref="EventBase"/>.</typeparam>
	/// <returns>A singleton instance of an event object of type <typeparamref name="TEventType"/>.</returns>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
	public TEventType GetEvent<TEventType>() where TEventType : EventBase, new()
	{
		lock (events) {
			EventBase existingEvent = null;

			if (!events.TryGetValue(typeof(TEventType), out existingEvent)) {
				var newEvent = new TEventType {
					SynchronizationContext = syncContext
				};
				events[typeof(TEventType)] = newEvent;

				return newEvent;
			} else {
				return (TEventType)existingEvent;
			}
		}
	}



	public void Publish<TEventType, TPayload>(TPayload action) where TEventType : PubSubEvent<TPayload>, new()
	{
		GetEvent<TEventType>().Publish(action);
	}
	public void Push<TEventType, TPayload, T>(T param)
			where TEventType : PubSubEvent<TPayload>, new()
			where TPayload : EventArgs
	{
		GetEvent<TEventType>().Publish(Activator.CreateInstance(typeof(TPayload), param) as TPayload);
	}
	public void Push<TEventType, TPayload>(params object[] p)
		where TEventType : PubSubEvent<TPayload>, new()
		where TPayload : EventArgs
	{
		GetEvent<TEventType>().Publish(Activator.CreateInstance(typeof(TPayload), p) as TPayload);
	}
	public void Push<TEventType, TPayload>(TPayload load)
			where TEventType : PubSubEvent<TPayload>, new()
	{
		GetEvent<TEventType>().Publish(load);
	}
}

//[Serializable]
//public class EventArgs<T>
//{
//    public static readonly EventArgs<T> Empty = new EventArgs<T>();

//    public EventArgs(T? t = default)
//    {
//    }
//}

///<summary>
/// Defines a base class to publish and subscribe to events.
///</summary>
public abstract class EventBase {
	private readonly List<IEventSubscription> _subscriptions = [];

	/// <summary>
	/// Allows the SynchronizationContext to be set by the EventAggregator for UI Thread Dispatching
	/// </summary>
	public SynchronizationContext SynchronizationContext { get; set; }

	/// <summary>
	/// Gets the list of current subscriptions.
	/// </summary>
	/// <value>The current subscribers.</value>
	protected ICollection<IEventSubscription> Subscriptions {
		get { return _subscriptions; }
	}

	/// <summary>
	/// Adds the specified <see cref="IEventSubscription"/> to the subscribers' collection.
	/// </summary>
	/// <param name="eventSubscription">The subscriber.</param>
	/// <returns>The <see cref="SubscriptionToken"/> that uniquely identifies every subscriber.</returns>
	/// <remarks>
	/// Adds the subscription to the internal list and assigns it a new <see cref="SubscriptionToken"/>.
	/// </remarks>
	public virtual SubscriptionToken InternalSubscribe(IEventSubscription eventSubscription)
	{
		if (eventSubscription == null) throw new ArgumentNullException(nameof(eventSubscription));

		eventSubscription.SubscriptionToken = new SubscriptionToken(Unsubscribe);

		lock (Subscriptions) {
			Subscriptions.Add(eventSubscription);
		}
		return eventSubscription.SubscriptionToken;
	}

	/// <summary>
	/// Calls all the execution strategies exposed by the list of <see cref="IEventSubscription"/>.
	/// </summary>
	/// <param name="arguments">The arguments that will be passed to the listeners.</param>
	/// <remarks>Before executing the strategies, this class will prune all the subscribers from the
	/// list that return a <see langword="null" /> <see cref="Action{T}"/> when calling the
	/// <see cref="IEventSubscription.GetExecutionStrategy"/> method.</remarks>
	public virtual void InternalPublish(params object[] arguments)
	{
		var executionStrategies = PruneAndReturnStrategies();
		foreach (var executionStrategy in executionStrategies) {
			executionStrategy(arguments);
		}
	}

	/// <summary>
	/// Removes the subscriber matching the <see cref="SubscriptionToken"/>.
	/// </summary>
	/// <param name="token">The <see cref="SubscriptionToken"/> returned by <see cref="EventBase"/> while subscribing to the event.</param>
	public virtual void Unsubscribe(SubscriptionToken token)
	{
		lock (Subscriptions) {
			var subscription = Subscriptions.FirstOrDefault(evt => evt.SubscriptionToken == token);
			if (subscription != null) Subscriptions.Remove(subscription);
		}
	}

	/// <summary>
	/// Returns <see langword="true"/> if there is a subscriber matching <see cref="SubscriptionToken"/>.
	/// </summary>
	/// <param name="token">The <see cref="SubscriptionToken"/> returned by <see cref="EventBase"/> while subscribing to the event.</param>
	/// <returns><see langword="true"/> if there is a <see cref="SubscriptionToken"/> that matches; otherwise <see langword="false"/>.</returns>
	public virtual bool Contains(SubscriptionToken token)
	{
		lock (Subscriptions) {
			var subscription = Subscriptions.FirstOrDefault(evt => evt.SubscriptionToken == token);
			return subscription != null;
		}
	}

	private List<Action<object[]>> PruneAndReturnStrategies()
	{
		List<Action<object[]>> returnList = [];

		lock (Subscriptions) {
			for (var i = Subscriptions.Count - 1; i >= 0; i--) {
				var listItem =
						_subscriptions[i].GetExecutionStrategy();

				if (listItem == null)           // Prune from main list. Log?
					_subscriptions.RemoveAt(i);
				else {
					returnList.Add(listItem);
				}
			}
		}

		return returnList;
	}

	/// <summary>
	/// Forces the PubSubEvent to remove any subscriptions that no longer have an execution strategy.
	/// </summary>
	public void Prune()
	{
		lock (Subscriptions) {
			for (var i = Subscriptions.Count - 1; i >= 0; i--) {
				if (_subscriptions[i].GetExecutionStrategy() == null) _subscriptions.RemoveAt(i);
			}
		}
	}
}

//
// Summary:
//     Defines a contract for an event subscription to be used by Prism.Events.EventBase.
public interface IEventSubscription {
	//
	// Summary:
	//     Gets or sets a Prism.Events.IEventSubscription.SubscriptionToken that identifies
	//     this Prism.Events.IEventSubscription.
	//
	// Value:
	//     A token that identifies this Prism.Events.IEventSubscription.
	SubscriptionToken SubscriptionToken { get; set; }

	//
	// Summary:
	//     Gets the execution strategy to publish this event.
	//
	// Returns:
	//     An System.Action`1 with the execution strategy, or null if the Prism.Events.IEventSubscription
	//     is no longer valid.
	Action<object[]> GetExecutionStrategy();
}

public class SubscriptionToken : IEquatable<SubscriptionToken>, IDisposable {
	private readonly Guid _token;
	private Action<SubscriptionToken> _unsubscribeAction;

	/// <summary>
	/// Initializes a new instance of <see cref="SubscriptionToken"/>.
	/// </summary>
	public SubscriptionToken(Action<SubscriptionToken> unsubscribeAction)
	{
		_unsubscribeAction = unsubscribeAction;
		_token = Guid.NewGuid();
	}

	///<summary>
	///Indicates whether the current object is equal to another object of the same type.
	///</summary>
	///<returns>
	///<see langword="true"/> if the current object is equal to the <paramref name="other" /> parameter; otherwise, <see langword="false"/>.
	///</returns>
	///<param name="other">An object to compare with this object.</param>
	public bool Equals(SubscriptionToken other)
	{
		if (other == null) return false;
		return Equals(_token, other._token);
	}

	///<summary>
	///Determines whether the specified <see cref="T:System.Object" /> is equal to the current <see cref="T:System.Object" />.
	///</summary>
	///<returns>
	///true if the specified <see cref="T:System.Object" /> is equal to the current <see cref="T:System.Object" />; otherwise, false.
	///</returns>
	///<param name="obj">The <see cref="T:System.Object" /> to compare with the current <see cref="T:System.Object" />. </param>
	///<exception cref="T:System.NullReferenceException">The <paramref name="obj" /> parameter is null.</exception><filterpriority>2</filterpriority>
	public override bool Equals(object obj)
	{
		if (ReferenceEquals(this, obj)) return true;
		return Equals(obj as SubscriptionToken);
	}

	/// <summary>
	/// Serves as a hash function for a particular type. 
	/// </summary>
	/// <returns>
	/// A hash code for the current <see cref="T:System.Object" />.
	/// </returns>
	/// <filterpriority>2</filterpriority>
	public override int GetHashCode()
	{
		return _token.GetHashCode();
	}

	/// <summary>
	/// Disposes the SubscriptionToken, removing the subscription from the corresponding <see cref="EventBase"/>.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "Should never have need for a finalizer, hence no need for Dispose(bool).")]
	public virtual void Dispose()
	{
		// While the SubscriptionToken class implements IDisposable, in the case of weak subscriptions 
		// (i.e. keepSubscriberReferenceAlive set to false in the Subscribe method) it's not necessary to unsubscribe,
		// as no resources should be kept alive by the event subscription. 
		// In such cases, if a warning is issued, it could be suppressed.

		if (_unsubscribeAction != null) {
			_unsubscribeAction(this);
			_unsubscribeAction = null;
		}

		GC.SuppressFinalize(this);
	}
}


