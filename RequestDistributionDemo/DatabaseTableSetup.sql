USE [RequestDistribution]
GO
/****** Object:  Table [dbo].[APIKey]    Script Date: 6/28/2019 10:41:38 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[APIKey](
	[APIKeyID] [int] IDENTITY(1,1) NOT NULL,
	[APIKey] [uniqueidentifier] NOT NULL,
	[UserName] [nvarchar](100) NOT NULL,
	[ProxiesRequested] [bigint] NOT NULL,
	[SuccessesReported] [bigint] NOT NULL,
	[SiteFailuresReported] [bigint] NOT NULL,
	[ProxyFailuresReported] [bigint] NOT NULL,
	[BansReported] [bigint] NOT NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Proxy]    Script Date: 6/28/2019 10:41:39 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Proxy](
	[ProxyId] [bigint] IDENTITY(1,1) NOT NULL,
	[URL] [nvarchar](255) NULL,
	[TotalSuccesses] [int] NULL,
	[TotalFailures] [int] NULL,
	[Score] [int] NULL,
	[Source] [nvarchar](255) NULL,
	[LastSession] [uniqueidentifier] NULL,
	[AddedDate] [datetime2](7) NULL,
	[Country] [nvarchar](255) NULL,
	[Streak] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[ProxyId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProxySiteScore]    Script Date: 6/28/2019 10:41:39 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProxySiteScore](
	[ProxySiteScoreId] [bigint] IDENTITY(1,1) NOT NULL,
	[Site] [nvarchar](255) NULL,
	[Successes] [int] NULL,
	[Failures] [int] NULL,
	[Score] [int] NULL,
	[Banned] [bit] NULL,
	[ProxyId] [bigint] NULL,
	[Streak] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[ProxySiteScoreId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[APIKey] ADD  DEFAULT ((0)) FOR [ProxiesRequested]
GO
ALTER TABLE [dbo].[APIKey] ADD  DEFAULT ((0)) FOR [SuccessesReported]
GO
ALTER TABLE [dbo].[APIKey] ADD  DEFAULT ((0)) FOR [SiteFailuresReported]
GO
ALTER TABLE [dbo].[APIKey] ADD  DEFAULT ((0)) FOR [ProxyFailuresReported]
GO
ALTER TABLE [dbo].[APIKey] ADD  DEFAULT ((0)) FOR [BansReported]
GO
ALTER TABLE [dbo].[ProxySiteScore]  WITH CHECK ADD  CONSTRAINT [FK9440A008A3A4F19F] FOREIGN KEY([ProxyId])
REFERENCES [dbo].[Proxy] ([ProxyId])
GO
ALTER TABLE [dbo].[ProxySiteScore] CHECK CONSTRAINT [FK9440A008A3A4F19F]
GO
