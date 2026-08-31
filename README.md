# 🚀 UserManagementMvc

**UserManagementMvc** is a database-driven ASP.NET Core MVC application designed as a complete User Management and Communication System.

The project started as a user management application and has gradually evolved into a broader platform containing secure authentication, registration verification, password recovery, role-based administration, dynamic website content, internal and external email communication, audit tracking, reporting and a real-time internal messenger.

The main objective of the project is to understand and implement real-world backend workflows instead of relying heavily on hard-coded application behavior.

---

## 🛠️ Technology Stack

- ASP.NET Core MVC
- C#
- Entity Framework Core
- MySQL
- Database-First oriented architecture
- HTML5
- CSS3
- Bootstrap
- JavaScript
- SignalR
- MailKit
- MimeKit
- SMTP
- IMAP
- ASP.NET Core Sessions
- Microsoft PasswordHasher
- QuestPDF

---

# 👤 User Management System

The application provides centralized management of registered users.

The system supports multiple account types including:

- 👤 User
- 🛡️ Admin
- 👑 SuperAdmin

Users can be created, updated, searched, activated, deactivated, soft-deleted, restored and managed according to role permissions.

Deleted users can be maintained separately instead of immediately removing their database records.

---

# 🔐 Multi-Identifier Login System

Users are not restricted to a single login identifier.

A user can sign in using:

📧 Email Address  
👤 Username  
📱 Mobile Number  
🌍 Country Code + Mobile Number

The authentication system identifies the account from any of these supported login values and then securely validates the password hash.

---

# 🛡️ Login Security & Account Blocking

The login system includes failed-attempt protection.

For normal Users and Admin accounts:

- Failed login attempts are tracked.
- After repeated invalid password attempts, the account can automatically become blocked.
- Blocked accounts cannot continue logging in.
- A SuperAdmin can review blocked accounts and unblock them.

SuperAdmin accounts can follow separate administrative rules.

Successful authentication resets applicable failed-login counters.

---

# 📝 Secure User Registration

The registration workflow includes multiple validation layers instead of directly inserting a user into the database.

Registration supports:

- Name validation
- Email validation
- Email availability checking
- Unique email enforcement
- Mobile number validation
- Country-specific mobile validation
- Date of birth validation
- Minimum age validation
- Password confirmation
- Department selection
- Security question configuration
- Unique username generation

---

# 👤 Automatic Unique Username Generation

Users can receive a unique username automatically when one is not manually provided.

The system creates a clean username based on the user's name and verifies its availability against the database.

If the generated username already exists, additional unique values are generated until an available username is found.

This avoids manually hard-coding usernames and reduces duplicate username conflicts.

---

# 📱 Country-Based Mobile Number Validation

Mobile numbers are validated according to the selected country code.

The application supports country-specific validation rules such as:

- 🇮🇳 India
- 🇺🇸 United States
- 🇬🇧 United Kingdom
- 🇳🇵 Nepal
- 🇦🇪 UAE
- 🇵🇰 Pakistan
- 🇧🇩 Bangladesh

The appropriate mobile number length and validation pattern can be applied according to the selected country.

---

# 🎂 Date of Birth Validation

The registration system validates Date of Birth before allowing registration.

The application prevents:

- Future dates
- Invalid dates
- Registration below the configured minimum age

This validation is implemented on both the user experience and backend workflow level.

---

# 📧 Registration Email Verification

Registration does not immediately create an active permanent account.

The application first stores pending registration information temporarily and sends a secure OTP to the user's email address.

After successful OTP verification, the final account information is saved to the database.

This creates a verified registration workflow instead of allowing unverified email registrations.

---

# 🔢 Secure OTP System

The application includes a reusable OTP verification architecture.

OTP functionality includes:

- Secure 6-digit OTP generation
- OTP hashing before storage
- OTP expiration
- OTP attempt limits
- Resend OTP
- Previous OTP invalidation
- Used OTP prevention
- Separate OTP purposes

Plain OTP values are not stored directly in the database.

---

# 🔑 Forgot Password & Account Recovery

The application provides multiple account recovery options.

A user can first search for an account using:

- Username
- Email
- Mobile Number
- Country Code + Mobile

After finding the account, recovery can continue using supported verification methods.

---

# 📧 Password Reset Through Email OTP

For email-based password recovery:

1. The registered account is located.
2. A secure OTP is generated.
3. The OTP is sent to the registered email.
4. The OTP is validated.
5. Reset permission is temporarily stored in the session.
6. The user can create a new password only after successful OTP verification.

Expired, invalid or repeatedly failed OTP attempts cannot be reused.

---

# ❓ Security Question Recovery

The system also supports password recovery through security questions.

Users can configure multiple security questions during registration.

Security answers are stored securely using hashing instead of plain text.

During account recovery, submitted answers are validated before password reset is permitted.

---

# 🌐 Dynamic Website Content Management

The project is designed to reduce unnecessary hard-coded content.

Website content can be managed through database-backed structures.

Dynamic sections can be used for areas such as:

🏠 Home Page  
📄 Privacy Page  
❓ Help Page  
🌐 Site Content

Instead of changing Razor files every time website content changes, authorized administrators can manage relevant content through application interfaces.

---

# 👑 Role-Based Administration

Different users receive different capabilities based on their roles.

### 👑 SuperAdmin

The SuperAdmin can have access to advanced administrative functionality such as:

- Complete user management
- Add users
- Manage roles
- Trash management
- Blocked account management
- Security question management
- Website content management
- Administrative reporting
- System monitoring

### 🛡️ Admin

Admins can manage allowed users and application functionality according to their permission level.

### 👤 User

Normal users receive access to their personal account and communication-related functionality.

---

# 🏢 Department-Based System

Users can belong to departments.

Department information can be used across administrative workflows, user searches and communication features.

This allows the application to behave more like an organizational or company-based management platform instead of a simple public user list.

---

# 📊 Dashboard

The application contains role-aware dashboard functionality.

Dashboard options can change depending on whether the logged-in account is:

- SuperAdmin
- Admin
- User
- Department-authorized account

Management options are exposed according to the user's access level.

---

# 📝 Audit Logging

Important activities can be recorded through the Audit Log system.

Audit records can include information such as:

- User
- Action
- Description
- IP Address
- Date and time

This makes administrative activity easier to review and provides better traceability for important application operations.

---

# 📊 Reporting & Export

The project includes reporting/export functionality for administrative use.

Data can be exported in formats such as:

📄 PDF  
📊 Excel

These reports can provide detailed information for administrative review, including system/user activity depending on the report being generated.

---

# 📧 Custom Email Management System

The project contains its own mailbox interface instead of using email only for OTP notifications.

Users can work with email through a dedicated mail module.

Core functionality includes:

📥 Inbox  
📤 Sent Mail  
✍️ Compose  
📝 Drafts  
⭐ Starred / Important Mail  
🗑️ Trash  
♻️ Restore  
❌ Permanent Delete  
↩️ Reply  
💬 Email conversation/thread relationships

---

# 📨 Internal User-to-User Email

Registered users can communicate with other users inside the application.

Internal email records are stored in the database so that communication can be displayed through the application's own Inbox and Sent Mail interfaces.

---

# 🌐 External Email Sending

The mail system is not restricted to users registered inside the project.

An application user can also send an email to an external email address.

External email sending is handled through SMTP using the configured mail infrastructure.

---

# 📤 SMTP Email Infrastructure

Outgoing email communication is implemented using:

- MailKit
- MimeKit
- SMTP
- Configurable SMTP settings
- Dedicated email service abstraction

Email infrastructure is separated from controllers through services, helping keep application responsibilities organized.

---

# 📥 IMAP External Incoming Email

The project also includes infrastructure for processing external incoming email.

IMAP-based mailbox processing can retrieve messages received by the configured external mailbox and import them into the application's mail system.

A background incoming-mail service can periodically check the mailbox while the application is running.

External messages can then be persisted inside the same database-backed mailbox environment used by the application.

---

# 🔁 Duplicate Incoming Email Protection

External emails can include an External Message ID.

The application can maintain a unique database index for this external message identifier.

This prevents the same email from being repeatedly imported into the application when the incoming mailbox is checked multiple times.

---

# 🗑️ Mail Trash & Permanent Delete

Mail deletion is implemented with mailbox-side state rather than immediately deleting the shared message record.

A sender or recipient can remove a message from their own mailbox independently.

For example:

- Sender deletes from their Sent view.
- Recipient can still retain their own copy.
- Deleted mail can move to Trash.
- Mail can be restored when applicable.
- Mail can later be permanently deleted for that user's own mailbox state.

This provides behavior closer to real email applications.

---

# ⭐ Starred Mail & Draft Management

Important messages can be marked as starred.

Users can also save unfinished messages as drafts and continue editing or sending them later.

Mail state is maintained through the database instead of being represented only in the user interface.

---

# 💬 Real-Time Internal Messenger

In addition to email, the application contains a dedicated internal Messenger system.

The Messenger is designed for direct communication between registered users in the system.

Users can search other active users and open private conversations.

---

# ⚡ SignalR Real-Time Chat

The Messenger uses SignalR to provide real-time communication.

This means users do not need to manually refresh the page every time a message is sent.

Messages can appear immediately when communication occurs between connected users.

---

# 🔎 Live User Search

Messenger search can dynamically locate registered users.

Users can search based on information such as:

- Name
- Username
- Email

The search experience is designed to update while the user types, making it faster to locate another user.

---

# ⬆️ Recent Conversation Ordering

Messenger conversations can be organized based on the most recent message activity.

When a new message arrives or a user sends another message, that conversation can move toward the top of the conversation list.

This provides an experience similar to modern messaging applications.

---

# 🔔 Unread Message Notifications

Unread messages can be tracked for individual conversations.

The Messenger can display unread counts or indicators so users can quickly identify conversations containing new messages.

Opening the conversation updates the applicable read state.

---

# ✓ Message Delivery Status

The Messenger supports familiar message-state indicators.

✓ **Sent**  
The message has been successfully saved/sent by the application.

✓✓ **Delivered**  
The message has reached the recipient's active application connection.

🔵✓✓ **Seen**  
The recipient has opened/read the conversation.

Database timestamps can be used for:

- SentAt
- DeliveredAt
- SeenAt

This creates a more realistic messaging experience.

---

# 🟢 Online / Last Seen Support

User activity information can be used to display availability-related information such as:

- Online status
- Last seen time

This allows the Messenger to behave more like a modern internal communication application.

---

# 📎 Messenger Attachments

The Messenger is designed to support richer communication beyond plain text.

Users can work with communication types such as:

📎 Files  
🖼️ Images  
🎥 Videos  
📄 Documents

These features make the internal Messenger more useful for an organizational environment.

---

# 😀 Dynamic Emoji System

The Messenger includes an emoji-oriented communication experience.

Instead of restricting the system to only permanently hard-coded emoji choices, emoji content can be made manageable through the application.

Administrative control can allow new emoji options to be added or managed without repeatedly modifying application source code.

This helps keep the communication module extensible.

---

# 🗑️ Messenger Message Management

Messenger messages can maintain independent deletion states for participants.

This makes it possible to support behavior such as:

**Delete for me**

A user can remove a message from their own conversation without necessarily deleting the other participant's copy.

---

# 🔒 Configuration & Secret Management

Sensitive information such as:

- Database credentials
- SMTP passwords
- Email App Passwords

should not be hard-coded into source files or committed to GitHub.

ASP.NET Core User Secrets or environment-based configuration can be used during development and deployment.

This keeps credentials separate from public source code.

---

# 🗄️ Database-Driven Architecture

The project is heavily database-driven.

Important application information such as users, messages, emails, OTP records, audit records, security questions and website content is stored in MySQL.

The application uses Entity Framework Core to work between the ASP.NET Core application and the relational database.

The project follows a Database-First oriented development approach, with the database structure acting as an important part of application design.

---

# 🧩 Separation of Responsibilities

Larger features are kept in separate modules rather than placing all logic inside a single controller.

For example:

### Email

- MailController
- MailService
- IMailService
- EmailService
- IEmailService
- MailMessage
- IncomingMailService
- IncomingMailSettings
- Mail Views

### Messenger

- MessengerController
- MessengerService
- MessengerHub
- ChatMessage
- Messenger ViewModels
- Messenger Views
- Messenger JavaScript
- Messenger CSS

This separation makes the project easier to understand, test, debug and extend.

---

# 🔄 Application Communication Architecture

The application now provides two different communication styles.

### 📧 Email Communication

Designed for structured messages, mailbox storage, drafts, sent mail, external SMTP communication and incoming IMAP processing.

### 💬 Instant Messaging

Designed for fast real-time communication between registered users through SignalR.

Together, these features allow the User Management application to function more like an internal organizational communication platform.

---

# 🎯 Project Goals

The purpose of this project is to gain practical experience building a real-world ASP.NET Core MVC application involving:

- Secure authentication
- Database design
- Role-based access
- User management
- OTP verification
- Password recovery
- Email infrastructure
- SMTP communication
- IMAP processing
- Real-time SignalR communication
- Dynamic content
- Audit trails
- Reporting
- Session management
- Administrative workflows
- Modular backend architecture

Rather than focusing only on CRUD operations, the project focuses on connecting multiple application systems together into one integrated platform.

---

# 🚀 Current Direction

The application continues to evolve with a focus on:

- Improved real-time communication
- Better email synchronization
- Enhanced Messenger experience
- File and media communication
- More detailed notifications
- Dynamic administrative configuration
- Improved reporting and monitoring
- Better UI/UX
- Stronger security
- Reduced hard-coded behavior
- Cleaner separation between application modules

---

## 📌 Summary

**UserManagementMvc** is more than a basic user CRUD project.

It combines:

👤 User Management  
🔐 Authentication  
📧 OTP Verification  
🔑 Account Recovery  
🛡️ Role-Based Administration  
🏢 Department-Based Workflows  
📊 Dashboards  
📝 Audit Logging  
📄 PDF/Excel Reporting  
🌐 Dynamic Website Content  
📥 Internal Mail  
📤 External SMTP Mail  
📨 IMAP Incoming Mail  
⭐ Draft/Star/Trash Mail Management  
💬 Real-Time Messenger  
⚡ SignalR  
✓✓ Delivered/Seen Status  
🔔 Unread Notifications  
📎 File & Media Communication  
😀 Dynamic Emoji Management  

into a single database-driven ASP.NET Core MVC application.

The project is being developed as a practical implementation of how user management, security, administration and communication systems can work together inside a modern web application.
