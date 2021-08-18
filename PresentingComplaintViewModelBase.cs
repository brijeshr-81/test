// -----------------------------------------------------------------------
// <copyright file="PresentingComplaintViewModelBase.cs" company="Advanced Health & Care">
//  Copyright © Advanced Health & Care 2020
// </copyright>
// -----------------------------------------------------------------------

namespace Odyssey.Session.Client.UI.ViewModels.Common
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Threading;

    using eCover.Security.Model.Business;
    using GalaSoft.MvvmLight.Messaging;
    using Microsoft.Cui.Controls;
    using Odyssey.Audit.Client.API.Log;
    using Odyssey.Audit.Model;
    using Odyssey.Client.Common;
    using Odyssey.Client.Common.Containers;
    using Odyssey.Client.Common.Messages;
    using Odyssey.Client.Common.Observers;
    using Odyssey.Client.Common.State_Machine;
    using Odyssey.Clinical.Model.Business;
    using Odyssey.Demographics.Client.API;
    using Odyssey.Demographics.Client.UI;
    using Odyssey.Demographics.Server.Entities;
    using Odyssey.eCover.Security.API.Users;
    using Odyssey.Session.Client.API.Clinical;
    using Odyssey.Session.Client.UI.DTOs;
    using Odyssey.Session.Client.UI.Messages;
    using SecurityModel = Odyssey.eCover.Security.Model.Business;
    
    /// <summary>
    /// Presenting complaint view model class base for both in process and stand alone
    /// </summary>
    public abstract class PresentingComplaintViewModelBase : INotifyPropertyChanged, IStateObserver
    {
        /// <summary>
        /// Object observes the application state
        /// </summary>
        private readonly IPropertyObserver observer;

        /// <summary>
        /// The user service caller
        /// </summary>
        private readonly IServiceCaller<IUserService> userServiceCaller =
            ObjectContainer.GetObject<IServiceCaller<IUserService>>();

        /// <summary>
        /// Field holds the protocols
        /// </summary>
        private ObservableCollection<Protocol> protocols;

        /// <summary>
        /// The session types
        /// </summary>
        private ObservableCollection<SessionType> sessionTypes;

        /// <summary>
        /// Field holds the complaints
        /// </summary>
        private ObservableCollection<Odyssey.Clinical.Model.Business.PresentingComplaint> presentingComplaints;

        /// <summary>
        /// Field holds the current protocol
        /// </summary>
        private Protocol protocol;

        /// <summary>
        /// Field holds the current presenting complaint
        /// </summary>
        private Odyssey.Clinical.Model.Business.PresentingComplaint presentingComplaint;

        /// <summary>
        /// Field holds the current presenting complaint from search result
        /// </summary>
        private Odyssey.Clinical.Model.Business.PresentingComplaint presentingComplaintSearchResult;

        /// <summary>
        /// Field holds the current presenting complaint from protocol list
        /// </summary>
        private Odyssey.Clinical.Model.Business.PresentingComplaint presentingComplaintProtocol;

        /// <summary>
        /// Field to hold the current header
        /// </summary>
        private Header header;

        /// <summary>
        /// Holds the current protocol
        /// </summary>
        private Session session;

        /// <summary>
        /// Field behind the CurrentSelection property
        /// </summary>
        private string currentSelection;

        /// <summary>
        /// The current dispatcher
        /// </summary>
        private Dispatcher dispatcher;

        /// <summary>
        /// Command to commit the presenting complaint
        /// </summary>
        private ICommand commitPC;

        /// <summary>
        /// Command to discard the presenting complaint
        /// </summary>
        private ICommand discardPC;

        /// <summary>
        /// The Command to proceed to outcome
        /// </summary>
        private ICommand proceedOutcome;

        /// <summary>
        /// determines if we need the contact filled in.
        /// </summary>
        private bool contactRequired;

        /// <summary>
        /// determines if call type is selected.
        /// </summary>
        private bool sessionTypeRequired;

        /// <summary>
        /// Don't know.
        /// </summary>
        private bool addingToSession;

        /// <summary>
        /// Backing field for the SearchCriteria property
        /// </summary>
        private string searchCriteria;

        // Removed as not assigned, likely as a result of chronic code removed elsewhere
        /////// <summary>
        /////// Chronic question adorner
        /////// </summary>
        ////private CentredAdorner chronicQuestionAdorner;

        /////// <summary>
        /////// Chronic question view
        /////// </summary>
        ////private ChronicQuestion chronicQuestion;

        /// <summary>
        /// The accept text for continue to telephone assessment
        /// </summary>
        private string acceptTextForTeleAssess;

        /// <summary>
        /// The continue to face to face button text
        /// </summary>
        private string acceptTextForFaceToFace;

        /// <summary>
        /// Settings provider (current domain)
        /// </summary>
        private IDomainSettingsProvider settingsProvider;

        /// <summary>
        /// The clinical service caller.
        /// </summary>
        private IServiceCaller<IClinicalSessionService> clinicalServiceCaller;

        /// <summary>
        /// Whether to show the mandatory details overlay
        /// </summary>
        private bool showMandatoryDetailsOverlay;

        /// <summary>
        /// The cancel mandatory details overlay command
        /// </summary>
        private ICommand cancelMandatoryDetailsOverlay;

        /// <summary>
        /// Whether the call type is visible on the missing details overlay
        /// </summary>
        private bool missingCallTypeInfoVisibility;

        /// <summary>
        /// Whether the contact details is visible on the missing details overlay
        /// </summary>
        private bool missingContactInfoVisibility;

        /// <summary>
        /// Stores the previous value of the contact information while the mandatory details overlay is displayed
        /// </summary>
        private string oldContactInfo;

        /// <summary>
        /// Stores the previous value of the call type information while the mandatory details overlay is displayed
        /// </summary>
        private SessionType oldCallTypeInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="PresentingComplaintViewModelBase"/> class.
        /// </summary>
        /// <param name="addingToSession">if set to <c>true</c> [adding to session].</param>
        protected PresentingComplaintViewModelBase(bool addingToSession)
        {
            this.InitialiseSessionforView();
            Messenger.Default.Register<CurrentLocationSelectedMessage>(this, this.UpdateSessionLocation);

            SecurityModel.Domain domain = this.userServiceCaller.CallServiceMethod(d => d.GetSiteSettings());
            this.ContactRequired = domain.PhoneNumberSetting == SecurityModel.PhoneNumberSetting.VisibleMandatory;
            this.AddingToSession = addingToSession;
            this.LoadProtocols();
            this.LoadTypes();
            this.observer = ObjectContainer.GetObject<IPropertyObserver>();
            this.observer.Subscribe(this.StateCallback);
            this.SessionTypeRequired = domain.CallTypeSetting == SecurityModel.CallTypeSetting.VisibleMandatory;
         }

        /// <summary>
        /// Initializes a new instance of the <see cref="PresentingComplaintViewModelBase"/> class.
        /// </summary>
        protected PresentingComplaintViewModelBase()
            : this(false)
        {
        }

        /// <summary>
        /// Event fired when a property changes
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets a value indicating whether we are adding a complaint to the session
        /// </summary>
        public bool AddingToSession
        {
            get
            {
                return this.addingToSession;
            }

            set
            {
                this.addingToSession = value;
                this.Notify("AddingToSession");
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether session type is required to proceed to OTA or face to face.
        /// </summary>
        public bool SessionTypeRequired
        {
            get
            {
                return this.sessionTypeRequired;
            }

            set
            {
                this.sessionTypeRequired = value;
                this.Notify("SessionTypeRequired");
            }
        }

        /// <summary>
        /// Gets the settings provider (current domain).
        /// </summary>
        public IDomainSettingsProvider SettingsProvider
        {
            get
            {
                return this.settingsProvider
                       ?? (this.settingsProvider = ObjectContainer.GetObject<IDomainSettingsProvider>());
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether contact required to proceed to OTA or face to face.
        /// </summary>
        public bool ContactRequired
        {
            get
            {
                return this.contactRequired;
            }

            set
            {
                this.contactRequired = value;
                this.Notify("ContactRequired");
            }
        }

        /// <summary>
        /// Gets or sets the list of protocols
        /// </summary>
        public ObservableCollection<Protocol> Protocols
        {
            get
            {
                return this.protocols;
            }

            set
            {
                this.protocols = value;
                this.Notify("Protocols");
            }
        }

        /// <summary>
        /// Gets the clinical service caller.
        /// </summary>
        public IServiceCaller<IClinicalSessionService> ClinicalServiceCaller
        {
            get
            {
                return this.clinicalServiceCaller
                       ?? (this.clinicalServiceCaller =
                           ObjectContainer.GetObject<IServiceCaller<IClinicalSessionService>>());
            }
        }

        /// <summary>
        /// Gets the accept text for the continue to tele assess button.
        /// </summary>
        public virtual string AcceptTextForTeleAssess
        {
            get
            {
                if (string.IsNullOrEmpty(this.acceptTextForTeleAssess))
                {
                    var manager = ObjectContainer.GetObject<IAuthorizationManager>();
                    if (manager.Check(ClaimsDictionary.CanAssessFaceToFace)
                        && manager.Check(ClaimsDictionary.CanAssessOnTelephone))
                    {
                        this.acceptTextForTeleAssess = Properties.Resources.ContinueToTeleAssess;
                    }
                    else
                    {
                        this.acceptTextForTeleAssess = Properties.Resources.ContinueText;
                    }
                }

                return this.acceptTextForTeleAssess;
            }
        }

        /// <summary>
        /// Gets the accept text for the continue to face to face button.
        /// </summary>
        public string AcceptTextForFaceToFace
        {
            get
            {
                if (string.IsNullOrEmpty(this.acceptTextForFaceToFace))
                {
                    var manager = ObjectContainer.GetObject<IAuthorizationManager>();
                    if (manager.Check(ClaimsDictionary.CanAssessFaceToFace)
                        && manager.Check(ClaimsDictionary.CanAssessOnTelephone))
                    {
                        this.acceptTextForFaceToFace = Properties.Resources.ContinueToFaceToFace;
                    }
                    else
                    {
                        this.acceptTextForFaceToFace = Properties.Resources.ContinueText;
                    }
                }

                return this.acceptTextForFaceToFace;
            }
        }

        /// <summary>
        /// Gets or sets the session types.
        /// </summary>
        /// <value>The session types.</value>
        public ObservableCollection<SessionType> SessionTypes
        {
            get
            {
                return this.sessionTypes;
            }

            set
            {
                this.sessionTypes = value;
                this.Notify("SessionTypes");
            }
        }

        /// <summary>
        /// Gets he visibility for the accept button for Tele assess
        /// </summary>
        public virtual Visibility AcceptForTeleAssessVisibility
        {
            get
            {
                var manager = ObjectContainer.GetObject<IAuthorizationManager>();
                return manager.Check(ClaimsDictionary.CanAssessOnTelephone) || manager.Check(ClaimsDictionary.CanHandleCalls) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Gets the visibility for the accept (for reception session) button
        /// </summary>
        public virtual Visibility AcceptForReceptionVisibility
        {
            get
            {
                var manager = ObjectContainer.GetObject<IAuthorizationManager>();
                return manager.Check(ClaimsDictionary.CanTriage) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Gets he visibility for the accept button for face to face
        /// </summary>
        public virtual Visibility AcceptForFaceToFaceVisibility
        {
            get
            {
                var manager = ObjectContainer.GetObject<IAuthorizationManager>();
                return manager.Check(ClaimsDictionary.CanAssessFaceToFace) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Gets or sets the list of PresentingComplaints
        /// </summary>
        public ObservableCollection<Odyssey.Clinical.Model.Business.PresentingComplaint> PresentingComplaints
        {
            get
            {
                return this.presentingComplaints;
            }

            set
            {
                this.presentingComplaints = value;
                this.Notify("PresentingComplaints");
            }
        }

        /// <summary>
        /// Gets or sets the current protocol.
        /// </summary>
        public Protocol Protocol
        {
            get
            {
                return this.protocol;
            }

            set
            {
                if (this.protocol != value)
                {
                    this.protocol = value;
                    this.Notify("Protocol");
                    if (this.Protocol != null)
                    {
                        this.PresentingComplaintSearchResult = null;
                        this.PresentingComplaintProtocol = null;
                    }

                    if (this.presentingComplaint == null)
                    {
                        this.CurrentSelection = string.Empty;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the current complaint selection.
        /// </summary>
        public string CurrentSelection
        {
            get
            {
                return this.currentSelection;
            }

            set
            {
                if (this.currentSelection != value)
                {
                    this.currentSelection = value;
                    this.Notify("CurrentSelection");
                }
            }
        }

        /// <summary>
        /// Gets or sets the current PresentingComplaint.
        /// Clears the Header
        /// </summary>
        public Odyssey.Clinical.Model.Business.PresentingComplaint PresentingComplaint
        {
            get
            {
                return this.presentingComplaint;
            }

            set
            {
                if (this.presentingComplaint != value)
                {
                    this.presentingComplaint = value;
                    this.Notify("PresentingComplaint");
                    if (this.presentingComplaint != null)
                    {
                        this.CurrentSelection = this.presentingComplaint.Description;
                        this.Header = null;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the current PresentingComplaintSearchResult
        /// </summary>
        public Odyssey.Clinical.Model.Business.PresentingComplaint PresentingComplaintSearchResult
        {
            get
            {
                return this.presentingComplaintSearchResult;
            }

            set
            {
                if (this.presentingComplaintSearchResult != value)
                {
                    this.presentingComplaintSearchResult = value;
                    this.Notify("PresentingComplaintSearchResult");
                    if (this.presentingComplaintSearchResult != null)
                    {
                        this.Protocol = null;
                        this.PresentingComplaintProtocol = null;
                    }

                    this.PresentingComplaint = this.presentingComplaintSearchResult;
                }
            }
        }

        /// <summary>
        /// Gets or sets the current PresentingComplaintProtocol
        /// </summary>
        public Odyssey.Clinical.Model.Business.PresentingComplaint PresentingComplaintProtocol
        {
            get
            {
                return this.presentingComplaintProtocol;
            }

            set
            {
                if (this.presentingComplaintProtocol != value)
                {
                    this.presentingComplaintProtocol = value;
                    this.Notify("PresentingComplaintProtocol");
                    if (this.presentingComplaintProtocol != null)
                    {
                        this.PresentingComplaintSearchResult = null;
                    }

                    this.PresentingComplaint = this.presentingComplaintProtocol;
                }
            }
        }

        /// <summary>
        /// Gets or sets the current header, clears the PresentingComplaint
        /// </summary>
        public Header Header
        {
            get
            {
                return this.header;
            }

            set
            {
                if (!object.ReferenceEquals(this.header, value))
                {
                    this.header = value;
                    this.Notify("Header");
                    this.CurrentSelection = this.header.Description;
                    if (this.header != null)
                    {
                        this.PresentingComplaint = null;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the session
        /// </summary>
        public Session Session
        {
            get
            {
                return this.session;
            }

            set
            {
                this.session = value;
                this.Notify("Session");
            }
        }

        /// <summary>
        /// Gets the Search command
        /// </summary>
        public ICommand Search
        {
            get
            {
                return new DelegateCommand(this.SearchPresentingComplaintsForPerson, this.CanSearchPresentingComplaints, true);
            }
        }

        /// <summary>
        /// Gets or sets the search criteria entered by the user
        /// </summary>
        public string SearchCriteria
        {
            get
            {
                return this.searchCriteria;
            }

            set
            {
                this.searchCriteria = value;
                this.Notify("SearchCriteria");
            }
        }

        /// <summary>
        /// Gets the Commit Presenting Complaint command
        /// </summary>
        public ICommand CommitPC
        {
            get
            {
                return this.commitPC ?? (this.commitPC = new DelegateCommand(this.Commit, this.CanCommit, true));
            }
        }

        /// <summary>
        /// Gets or sets the type of the selected session.
        /// </summary>
        /// <value>The type of the selected session.</value>
        public SessionType SelectedSessionType
        {
            get
            {
                return this.ClinicalSession == null ? null : this.SessionTypes.FirstOrDefault(s => s.Description == this.ClinicalSession.SessionType);
            }

            set
            {
                if (this.ClinicalSession != null)
                {
                    this.ClinicalSession.SessionType = value == null ? null : value.Description;
                }             

                ObjectContainer.GetObject<ISessionTypeState>().SessionTypeId = value == null ? null : new Guid?(value.TypeId);
                this.Notify("SelectedSessionType");
            }
        }

        /// <summary>Gets the clinical session.</summary>
        public ClinicalSession ClinicalSession
        {
            get
            {
                var clinicalSession = this.Session as ClinicalSession;
                var queuedSession = this.Session as QueuedSession;
                if (clinicalSession == null && queuedSession != null)
                {
                    this.Session = clinicalSession = this.ClinicalServiceCaller.CallServiceMethod(s => s.GetSession(queuedSession.Id));
                }
                else if (clinicalSession == null && this.Session != null)
                {
                    this.Session = clinicalSession = this.ClinicalServiceCaller.CallServiceMethod(s => s.GetSession(this.session.Id));
                    var state = ObjectContainer.GetObject<ISessionState>();
                    state.Session = this.Session;
                }

                return clinicalSession;
            }
        }

        /// <summary>
        /// Gets the Discard Presenting Complaint command
        /// </summary>
        public ICommand DiscardPC
        {
            get
            {
                return this.discardPC ?? (this.discardPC = new DelegateCommand(this.DoDiscard, this.CanDiscard));
            }
        }

        /// <summary>
        /// Gets the Proceed to Outcome command
        /// </summary>
        public ICommand ProceedToOutcome
        {
            get
            {
                return this.proceedOutcome
                    ?? (this.proceedOutcome = new DelegateCommand(this.ProceedOutcome, this.CanExecuteProceedOutcome));
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance can proceed to outcome.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance can proceed to outcome; otherwise, <c>false</c>.
        /// </value>
        public bool CanProceedToOutcome
        {
            get
            {
                var manager = ObjectContainer.GetObject<IAuthorizationManager>();
                bool result = !manager.Check(ClaimsDictionary.CanTriage);

                return result;
            }
        }

        /// <summary>
        /// Gets the cancel mandatory details overlay command
        /// </summary>
        public ICommand CancelMandatoryDetailsOverlay
        {
            get
            {
                return this.cancelMandatoryDetailsOverlay
                    ?? (this.cancelMandatoryDetailsOverlay = new DelegateCommand(
                        o =>
                        {
                            this.Session.Contact = this.oldContactInfo;
                            this.SelectedSessionType = this.oldCallTypeInfo;
                            this.ShowMandatoryDetailsOverlay = false;
                        }));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show the mandatory details overlay
        /// </summary>
        public bool ShowMandatoryDetailsOverlay
        {
            get
            {
                return this.showMandatoryDetailsOverlay;
            }

            set
            {
                if (this.showMandatoryDetailsOverlay != value)
                {
                    this.showMandatoryDetailsOverlay = value;
                    if (this.showMandatoryDetailsOverlay)
                    {
                        this.oldContactInfo = this.Session.Contact;
                        this.oldCallTypeInfo = this.SelectedSessionType;
                        this.MissingContactInfoVisibility = this.Session != null && string.IsNullOrWhiteSpace(this.Session.Contact);
                        this.MissingCallTypeInfoVisibility = this.SelectedSessionType == null;
                    }

                    this.Notify(nameof(this.ShowMandatoryDetailsOverlay));
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the call type is visible in the mandatory details missing overlay
        /// </summary>
        public bool MissingCallTypeInfoVisibility
        {
            get
            {
                var domain = this.SettingsProvider.Domain;
                return domain.CallTypeSetting == CallTypeSetting.VisibleMandatory && this.missingCallTypeInfoVisibility;
            }

            set
            {
                this.missingCallTypeInfoVisibility = value;
                this.Notify(nameof(this.MissingCallTypeInfoVisibility));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the call back number is mandatory in the mandatory details missing overlay
        /// </summary>
        public bool MissingContactInfoVisibility
        {
            get
            {
                var domain = this.SettingsProvider.Domain;
                return domain.PhoneNumberSetting == PhoneNumberSetting.VisibleMandatory && this.missingContactInfoVisibility;
            }

            set
            {
                this.missingContactInfoVisibility = value;
                this.Notify(nameof(this.MissingContactInfoVisibility));
            }
        }

        /// <summary>
        /// Gets the dispatcher
        /// </summary>
        private Dispatcher Dispatcher
        {
            get
            {
                return this.dispatcher
                    ?? (this.dispatcher =
                        Application.Current != null ? Application.Current.Dispatcher : Dispatcher.CurrentDispatcher);
            }
        }

        #region IStateObserver Members

        /// <summary>
        /// State machine change notification
        /// </summary>
        /// <param name="currentState">The current state</param>
        public void Notification(State currentState)
        {
            // Code appears unused at the time, but is retained in case chronic functionality is reintroduced
            ////if (currentState.Model.GetType().Name == "Locked")
            ////{
            ////    this.chronicQuestionAdorner.Visibility = Visibility.Hidden;
            ////}
            ////else
            ////{
            ////    this.chronicQuestionAdorner.Visibility = Visibility.Visible;
            ////}
        }

        #endregion

        /// <summary>
        /// Process a double-click form the presenting complaint lists
        /// </summary>
        public void ProcessDoubleClick()
        {
            if (this.CommitPC.CanExecute(null))
            {
                this.CommitPC.Execute(null);
                return;
            }

            this.ShowMandatoryDetailsOverlay = true;
        }

        /// <summary>
        /// Called when the selection changes
        /// </summary>
        /// <param name="selection">Selected value</param>
        public void Select(object selection)
        {
            this.Header = selection as Header;
        }

        /// <summary>Audits the emergency note (if there is any)</summary>
        /// <param name="auditEntry">The audit Entry.</param>
        public abstract void AuditEmergencyNote(ref AuditEntry auditEntry);

        /// <summary>Unsubscribe from observer.</summary>
        /// <param name="stateCallback">The state call-back.</param>
        protected void Unsubscribe(Action<string, object> stateCallback)
        {
            this.observer.Unsubscribe(stateCallback);
        }

        /// <summary>
        /// Commit the header
        /// </summary>
        /// <param name="parameter">The input parameter. If it is the string <c>nochronic</c> the chronic adorner will not appear</param>
        protected virtual void Commit(object parameter)
        {            
            string action = this.GetActionIfUnknown(parameter);

            if (action == null)
            {
                this.ShowMandatoryDetailsOverlay = true;
                return;
            }

            // The following code has been commented out to disable chronic/acute functionality for now
            ////Header chronicHeader = null;
            ////bool disableChronic = parameter is string && (parameter as string) == "nochronic";
            ////IAuthorizationManager manager = ObjectContainer.GetObject<IAuthorizationManager>();
            ////if (manager.Check(ClaimsDictionary.CanAssess) && state.Session is ClinicalSession && !disableChronic)
            ////{
            ////    var getChronicHeaderFunction = ObjectContainer.GetObject<IFunction<Header>>(TaskInventory.GetChronicHeader);
            ////    chronicHeader = getChronicHeaderFunction.Execute(new object[] { this.PresentingComplaint, this.Header });
            ////}

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            ////if (chronicHeader != null)
                // ReSharper disable once HeuristicUnreachableCode
            ////{
            ////    var container = ObjectContainer.GetObject<IStateMachineContainer>();
            ////    var question = chronicHeader.Questions.FirstOrDefault();
            ////    if (this.chronicQuestionAdorner == null)
            ////    {
            ////        this.chronicQuestion = new ChronicQuestion
            ////        {
            ////            AnswerCommand = new DelegateCommand(a =>
            ////            {
            ////                this.chronicQuestionAdorner.RemoveAdornment();
            ////                container.StateMachine.UnSubscribe(this.GetType().Name);
            ////                Messenger.Default.Unregister(this);
            ////                Mouse.OverrideCursor = Cursors.Wait;
            ////                this.FinaliseSelectComplaint();
            ////                try
            ////                {
            ////                    var task = ObjectContainer.GetObject<IFunction<AnswerableSession>>(TaskInventory.AnswerMultipleChoiceQuestion);
            ////                    var result = task.Execute(new object[] { state.Session as AnswerableSession, new List<Answer> { a as Answer }, null });
            ////                    state.Session = result;
            ////                }
            ////                catch (FaultException<SessionFault>)
            ////                {
            ////                }
            ////                catch (Exception)
            ////                {
            ////                    throw;
            ////                }
            ////                finally
            ////                {
            ////                    Mouse.OverrideCursor = null;
            ////                }
            ////            }),
            ////            CancelCommand = new DelegateCommand(o =>
            ////            {
            ////                this.chronicQuestionAdorner.RemoveAdornment();
            ////                container.StateMachine.UnSubscribe(this.GetType().Name);
            ////                this.observer.Unsubscribe(this.StateCallback);
            ////            })
            ////        };

            ////        this.chronicQuestionAdorner = new CentredAdorner(this.chronicQuestion);
            ////    }

            ////    this.chronicQuestion.Question = question;
            ////    this.chronicQuestion.Answers = question.Answers.Skip(1).ToObservableCollection();
            ////    container.StateMachine.Subscribe(this);
            ////    this.chronicQuestionAdorner.AddAdornment();
            ////    return;
            ////}
            ////else
            ////{
                Mouse.OverrideCursor = Cursors.Wait;
                this.FinaliseSelectComplaint(action);
                Mouse.OverrideCursor = null;
            ////}
        }

        /// <summary>Complete the presenting complaint selection, after any chronic question adorner has been displayed</summary>
        /// <param name="action">The action.</param>
        protected virtual void FinaliseSelectComplaint(string action)
        {
            this.observer.Unsubscribe(this.StateCallback);

            ObjectContainer.GetObject<IEmailAdviceViewModel>()?.Reset();

            if (!this.AddingToSession)
            {
                // call new method to check existing presenting complaint with the current session presenting complaint if they are different send a message and audit
                this.ReportChangingPresentingComplaint();
                this.WriteInMemorySession(action);
            }

            Messenger.Default.Send(new SessionCreated());
            Messenger.Default.Send(new PresentingComplaintComplete { Complaint = this.PresentingComplaint, Header = this.Header, Action = action, Adding = this.AddingToSession });
    }

        /// <summary>
        /// The report changing presenting complaint.
        /// </summary>
        protected void ReportChangingPresentingComplaint()
        {
            if (string.IsNullOrEmpty(this.Session.PresentingComplaint) || this.Session.PresentingComplaint.Equals(this.PresentingComplaint.Description))
            {
                return;
            }

            var identity = ObjectContainer.GetObject<IIdentityState>().Identity;
            ObjectContainer.GetObject<ICommand>(TaskInventory.Audit).Execute(
                                   new object[]
                                        {
                                            new AuditEntry
                                                {
                                                    Summary = Properties.Resources.ChangePresentingComplaintAudit,
                                                    Description = string.Format(Properties.Resources.ChangePresentingComplaintAuditDescription, identity.Username, this.Session.PresentingComplaint, this.PresentingComplaint.Description),
                                                    Level = Properties.Resources.PresentingComplaintChanged
                                                },
                                           Properties.Resources.PresentingComplaintChanged
                                        });
            Messenger.Default.Send(new ChangePresentingComplaintMessage { CurrentPresentingComplaint = this.PresentingComplaint.Description, PreviousPresentingComplaint = this.Session.PresentingComplaint, UserName = identity.Username });
        }

        /// <summary>
        /// Call-back for state change observations
        /// </summary>
        /// <param name="name">Not sure what this is used for</param>
        /// <param name="value">Again not sure what this is used for</param>
        protected void StateCallback(string name, object value)
        {
            var person = value as Person;
            if (person != null)
            {
                this.PersonChanged(person);
            }
        }

        /// <summary>
        /// Initializes the session for view.
        /// </summary>
        protected virtual void InitialiseSessionforView()
        {
            var state = ObjectContainer.GetObject<ISessionState>();
            this.Session = state.Session;
        }

        /// <summary>Writes the in memory session to database.</summary>
        /// <param name="action">The action.</param>
        /// <remarks>Intended for use before the calls exiting this form run as they probably assume a session exists in the services layer.</remarks>
        protected void WriteInMemorySession(string action)
        {
            // Check what the session setup requirement of the previous state machine state is and act accordingly
            var container = ObjectContainer.GetObject<IStateMachineContainer>();
            switch (container.StateMachine.Current.Previous.Model.RequiresSessionSetup)
            {
                case SessionSetupType.Setup:
                    this.CreateSessionCameFromNewUserOrUseThisPatient();
                    break;
                case SessionSetupType.Pickup:
                    this.CreateSessionCameFromChangePresentingComplaint(action);
                    break;
            }

            this.UpdateClinicalSession();
        }

        /// <summary>
        /// Discard the presenting complaint
        /// </summary>
        /// <param name="parameter">The input parameter</param>
        private void DoDiscard(object parameter)
        {
            this.RecordEmergencyProtocolNote();

            if (this.observer != null)
            {
                this.observer.Unsubscribe(this.StateCallback);
            }

            if (this.AddingToSession)
            {
                Messenger.Default.Send(new NewProtocolCancel());                
            }
            else
            {
                Messenger.Default.Send(new PresentingComplaintCancel());
            }
        }

        /// <summary>Records the emergency protocol note.</summary>
        private void RecordEmergencyProtocolNote()
        {
            var emergencyProtocolNote = ObjectContainer.GetObject<IEmergencyProtocolState>().EmergencyProtocolNote;
            if (string.IsNullOrEmpty(emergencyProtocolNote.Text))
            {
                return;
            }

            var auditEntry = new AuditEntry
                {
                    Summary = Properties.Resources.EmergencyProtocolNote,
                    Description = emergencyProtocolNote.Text,
                    Level = Properties.Resources.EmergencyProtocolNote
                };

            this.AuditEmergencyNote(ref auditEntry);
            ObjectContainer.GetObject<IServiceCaller<IAuditService>>()
                .CallServiceMethod(d => d.Add(auditEntry, Properties.Resources.EmergencyProtocolNote));
        }

        /// <summary>
        /// Method which is called if the currently selected person changes
        /// </summary>
        /// <param name="p">The currently selected person</param>
        private void PersonChanged(Person p)
        {
            if (p == null || this.Session == null)
            {
                // not intersted in observing state any longer
                this.observer.Unsubscribe(this.StateCallback);
            }
            else
            {
                uint newAge = p.PersonCurrentDetail.Age;
                string newGender = p.PersonCurrentDetail.Gender == PatientGender.Female ? "F" : p.PersonCurrentDetail.Gender == PatientGender.Male ? "M" : " ";
                if (newAge != this.Session.Age || newGender != this.Session.Gender)
                {
                    this.Session.Gender = newGender;
                    this.Session.Age = newAge;
                    this.LoadProtocols();
                }
            }
        }

        /// <summary>
        /// Proceed directly to Outcome
        /// </summary>
        /// <param name="parameter">Unused object</param>
        private void ProceedOutcome(object parameter)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                if (this.Session != null)
                {
                    // As it is new call when Processed to Outcome is clicked directly, the session Id is empty, below steps will populate session Id and log call. 
                    if (this.Session.Id == Guid.Empty || !this.AddingToSession)
                    {
                        // call new method to check existing presenting complaint with the current session presenting complaint if they are different send a message and audit
                        this.ReportChangingPresentingComplaint();
                        this.WriteInMemorySession(parameter.ToString());
                        var state = ObjectContainer.GetObject<ISessionState>();
                        this.Session = state.Session;
                    }

                    var parameters = new object[2];
                    parameters[0] = this.Session;

                    if (this.PresentingComplaint != null)
                    {
                        parameters[1] = this.PresentingComplaint;
                    }
                    else if (this.Header != null)
                    {
                        parameters[1] = this.Header;
                    }

                    if (parameters[1] != null)
                    {
                        Messenger.Default.Send(new SessionCreated());
                        var task = ObjectContainer.GetObject<ICommand>(TaskInventory.SendToAssessmentTask);
                        task.Execute(parameters);
                    }
                }

                var summarytask = ObjectContainer.GetObject<ICommand>(TaskInventory.PerformAction);
                summarytask.Execute("Summary");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        /// <summary>
        /// Checks if the the user can proceed directly to outcome
        /// </summary>
        /// <param name="parameter">Unused parameter</param>
        /// <returns>true if this user has claim to proceed directly to outcome, false otherwise.</returns>
        private bool CanExecuteProceedOutcome(object parameter)
        {
            var manager = ObjectContainer.GetObject<IAuthorizationManager>();
            return this.CanCommit(parameter) && manager.Check(ClaimsDictionary.CanProceedToOutcome);
        }

        /// <summary>
        /// Load the protocols
        /// </summary>
        private void LoadProtocols()
        {
            WaitCallback callback = p =>
                {
                    var personState = ObjectContainer.GetObject<IPersonState>();
                    var person = personState.Person;
                    Clinical.Model.Business.Gender gender;

                    switch (person.PersonCurrentDetail.Gender)
                    {
                        case PatientGender.Male:
                            gender = Clinical.Model.Business.Gender.Male;
                            break;
                        case PatientGender.Female:
                            gender = Clinical.Model.Business.Gender.Female;
                            break;
                        case PatientGender.NotKnown:
                        case PatientGender.NotSpecified:
                            gender = Clinical.Model.Business.Gender.Unknown;
                            break;
                        default:
                            throw new InvalidOperationException("Unexpected gender");
                    }

                    var function = ObjectContainer.GetObject<IFunction<ObservableCollection<Protocol>>>(TaskInventory.GetProtocols);
                    var result = function.Execute(new GetProtocolsArguments(person.PersonCurrentDetail.Age, gender));
                    this.Dispatcher.BeginInvoke(new Action<object>(this.SetProtocols), result);
                };

            callback.Invoke(null);
            //// ThreadPool.QueueUserWorkItem(callback);
        }

        /// <summary>
        /// Load the session types
        /// </summary>
        private void LoadTypes()
        {
            var function = ObjectContainer.GetObject<IFunction<ObservableCollection<SessionType>>>(TaskInventory.GetSessionTypes);
            this.SessionTypes = function.Execute(true);
        }

        /// <summary>
        /// Updates the protocols (called on the UI thread)
        /// </summary>
        /// <param name="param">Protocols to set</param>
        private void SetProtocols(object param)
        {
            this.Protocols = param as ObservableCollection<Protocol>;
        }

        /// <summary>
        /// Determines if the commit presenting complaint command can run
        /// </summary>
        /// <param name="parameter">The input parameter</param>
        /// <returns>true if the presenting complaint can be committed, otherwise false</returns>
        private bool CanCommit(object parameter)
        {
            bool result = false;

            if (this.Header != null || this.PresentingComplaint != null || this.ShowMandatoryDetailsOverlay)
            {
                var manager = ObjectContainer.GetObject<IAuthorizationManager>();
                result = (manager.Check(ClaimsDictionary.CanAssessOnTelephone) ||
                          manager.Check(ClaimsDictionary.CanTriage) ||
                          manager.Check(ClaimsDictionary.CanAddToQueue)) &&
                          (this.Session.HasContact || !this.ContactRequired) &&
                          (this.SelectedSessionType != null || !this.sessionTypeRequired);
            }

            return result;
        }

        /// <summary>
        /// Given that there is no command parameter, determine what action Commit should take
        /// </summary>
        /// <param name="parameter">Command parameter passed to Commit</param>
        /// <returns>
        /// The parameter unchanged if it is not null, 
        /// otherwise the most appropriate parameter for the user, if there is a single unambiguous value
        /// otherwise null if the action cannot be exactly determined
        /// </returns>
        private string GetActionIfUnknown(object parameter)
        {
            var action = parameter as string;
            if (action != null)
            {
                return action;
            }

            var manager = ObjectContainer.GetObject<IAuthorizationManager>();
            bool teleAssess = manager.Check(ClaimsDictionary.CanAssessOnTelephone) || manager.Check(ClaimsDictionary.CanHandleCalls);
            bool faceToFace = manager.Check(ClaimsDictionary.CanAssessFaceToFace);
            bool reception = manager.Check(ClaimsDictionary.CanTriage);
            return teleAssess && !faceToFace && !reception
                ? "OkTeleAssess"
                : !teleAssess && faceToFace && !reception
                    ? "OkFaceToFace"
                    : !teleAssess && !faceToFace && reception
                        ? "Ok"
                        : null;
        }

        /// <summary>
        /// Update session
        /// </summary>
        /// <param name="session">Session to update</param>
        private void UpdateSession(Session session)
        {
            if (session == null)
            {
                return;
            }

            var emergencyProtocolNote = ObjectContainer.GetObject<IEmergencyProtocolState>().EmergencyProtocolNote;
            if (!string.IsNullOrEmpty(emergencyProtocolNote.Text) && session.Notes.OfType<EmergencyProtocolNote>().All(d => d.Text != emergencyProtocolNote.Text))
            {
                session.Notes.Add(emergencyProtocolNote);
            }

            ObjectContainer.GetObject<ICommand>(session.UpdateTask()).Execute(session);
        }

        /// <summary>
        /// Searches for any matching presenting complaints for the current person
        /// </summary>
        /// <param name="param">The input parameter</param>
        private void SearchPresentingComplaintsForPerson(object param)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                var personState = ObjectContainer.GetObject<IPersonState>();
                var person = personState.Person;

                if (this.session != null)
                {
                    var parameterArray = new object[] { person, this.SearchCriteria, PresentingComplaintSearchType.Contains, this.Session };
                    if (string.IsNullOrEmpty(this.SearchCriteria))
                    {
                        this.PresentingComplaints = null;
                    }
                    else
                    {
                        var function
                            = ObjectContainer.GetObject<IFunction<ObservableCollection<PresentingComplaint>>>(TaskInventory.SearchComplaintsForPerson);

                        ObservableCollection<PresentingComplaint> allMatchingPresentingComplaints;
                        this.PresentingComplaints = function.Execute(parameterArray);

                        if (this.PresentingComplaints.Count > 0)
                        {
                            allMatchingPresentingComplaints = this.PresentingComplaints;
                            
                            // Filter out the search results starting with the search criteria in alphabetical order
                            this.PresentingComplaints = new ObservableCollection<PresentingComplaint>(allMatchingPresentingComplaints.Where(pc => pc.Description.ToLower().StartsWith(this.SearchCriteria.ToLower())));
                            
                            // Pick up the remaining search results which has matching search criteria text anywhere that are already in alphabetical order
                            var presentingComplaintsNotStartingWithSearchCriteria = allMatchingPresentingComplaints.Where(pc2 => !this.PresentingComplaints.Any(pc1 => pc2.Description.Contains(pc1.Description)));

                            this.PresentingComplaints = new ObservableCollection<PresentingComplaint>(this.PresentingComplaints.Union(presentingComplaintsNotStartingWithSearchCriteria));
                        }
                    }
                }
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        /// <summary>
        /// Determines if can search (i.e. textbox has some criteria)
        /// </summary>
        /// <param name="param">The input parameter</param>
        /// <returns>True if can search, false otherwise</returns>
        private bool CanSearchPresentingComplaints(object param)
        {
            return !string.IsNullOrEmpty(this.SearchCriteria);
        }

        /// <summary>
        /// Notify of a property change
        /// </summary>
        /// <param name="property">Name of the property that changed</param>
        private void Notify(string property)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }

        /// <summary>
        /// Determines whether this instance can discard the session.
        /// </summary>
        /// <param name="p">unused parameter</param>
        /// <returns>
        ///     <c>true</c> if this instance can discard the session; otherwise, <c>false</c>.
        /// </returns>
        private bool CanDiscard(object p)
        {
            var state = ObjectContainer.GetObject<ISessionState>();
            bool result = state.Session != null
                && !(state.Session is ReceptionSession)
                && (string.IsNullOrEmpty(state.Session.PresentingComplaint) || this.AddingToSession);

            return result;
        }

        /// <summary>Update the session information on the server if this session is of a clinical type</summary>
        private void UpdateClinicalSession()
        {
            var clinicalSession = this.Session as ClinicalSession;
            if (clinicalSession == null)
            {
                return;
            }

            var applicationSettings = TinyIoC.TinyIoCContainer.Current.Resolve<Settings>();
            var state = ObjectContainer.GetObject<ISessionState>();
            var newSession = (ClinicalSession)state.Session;
            var currentSession = clinicalSession;
            newSession.SessionType = currentSession.SessionType;
            newSession.Contact = currentSession.Contact;
            newSession.AppVersion = applicationSettings.ApplicationVersion;

            var identity = ObjectContainer.GetObject<IIdentityState>();
            if (identity.Identity.Check(ClaimsDictionary.CanAssessFaceToFace)
                || identity.Identity.Check(ClaimsDictionary.CanAssessOnTelephone)
                || identity.Identity.Check(ClaimsDictionary.CanHandleCalls))
            {
                newSession.ClinicallyAssessed = true;
            }

            this.UpdateSession(newSession);
        }

        /// <summary>
        /// Manages the session creation when the user came from the search control.
        /// </summary>
        private void CreateSessionCameFromNewUserOrUseThisPatient()
        {
            // need the claim switches
            var task = ObjectContainer.GetObject<ICommand>(TaskInventory.CreateSession);
            task.Execute(null);
        }

        /// <summary>Creates the session when the process came from a change presenting complaint command (from confirmation controller)</summary>
        /// <param name="action">The action.</param>
        private void CreateSessionCameFromChangePresentingComplaint(string action)
        {
            this.PickupSchedule();
            var state = ObjectContainer.GetObject<ISessionState>();
            var queued = state.Session;

            switch (action)
            {
                case "OkFaceToFace":
                {
                    queued.IsFTF = true;
                    break;
                }

               default:
                {
                    queued.IsFTF = false;
                    break;
                }
            }

            var personState = ObjectContainer.GetObject<IPersonState>();
            if (personState != null && personState.CurrentLocationId != 0)
            {
                this.ClinicalServiceCaller.CallServiceMethod(
                    s => s.SetSessionCurrentAddressId(queued.Id, personState.CurrentLocationId));
            }

            var load = ObjectContainer.GetObject<ICommand>(TaskInventory.FillSession);
            load.Execute(queued);
        }

        /// <summary>
        /// Executes the pickup schedule task to start the session and store it in application state.
        /// </summary>
        private void PickupSchedule()
        {
            var scheduleState = ObjectContainer.GetObject<IScheduleState>();

            Schedule schedule = scheduleState.Schedule;
            var function = ObjectContainer.GetObject<IFunction<bool>>(TaskInventory.PickupSchedule);
            function.Execute(schedule);
        }

        /// <summary>
        /// Updates the current location ID in the session
        /// </summary>
        /// <param name="message">The current location message</param>
        private void UpdateSessionLocation(CurrentLocationSelectedMessage message)
        {
            ObjectContainer.GetObject<IPersonState>().CurrentLocationId = message.CurrentLocationId;
        }
    }
}
