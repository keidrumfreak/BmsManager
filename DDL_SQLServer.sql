USE [master]
GO

/****** Object:  Database [BmsManager]    Script Date: 2021/08/12 19:11:24 ******/
CREATE DATABASE [BmsManager]
 CONTAINMENT = NONE
 ON  PRIMARY 
( NAME = N'BmsManager', FILENAME = N'E:\.sys\Program Files\Microsoft SQL Server\MSSQL15.SQLEXPRESS\MSSQL\DATA\BmsManager.mdf' , SIZE = 73728KB , MAXSIZE = UNLIMITED, FILEGROWTH = 65536KB )
 LOG ON 
( NAME = N'BmsManager_log', FILENAME = N'E:\.sys\Program Files\Microsoft SQL Server\MSSQL15.SQLEXPRESS\MSSQL\DATA\BmsManager_log.ldf' , SIZE = 73728KB , MAXSIZE = 2048GB , FILEGROWTH = 65536KB )
 WITH CATALOG_COLLATION = DATABASE_DEFAULT
GO

IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [BmsManager].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO

ALTER DATABASE [BmsManager] SET ANSI_NULL_DEFAULT OFF 
GO

ALTER DATABASE [BmsManager] SET ANSI_NULLS OFF 
GO

ALTER DATABASE [BmsManager] SET ANSI_PADDING OFF 
GO

ALTER DATABASE [BmsManager] SET ANSI_WARNINGS OFF 
GO

ALTER DATABASE [BmsManager] SET ARITHABORT OFF 
GO

ALTER DATABASE [BmsManager] SET AUTO_CLOSE OFF 
GO

ALTER DATABASE [BmsManager] SET AUTO_SHRINK OFF 
GO

ALTER DATABASE [BmsManager] SET AUTO_UPDATE_STATISTICS ON 
GO

ALTER DATABASE [BmsManager] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO

ALTER DATABASE [BmsManager] SET CURSOR_DEFAULT  GLOBAL 
GO

ALTER DATABASE [BmsManager] SET CONCAT_NULL_YIELDS_NULL OFF 
GO

ALTER DATABASE [BmsManager] SET NUMERIC_ROUNDABORT OFF 
GO

ALTER DATABASE [BmsManager] SET QUOTED_IDENTIFIER OFF 
GO

ALTER DATABASE [BmsManager] SET RECURSIVE_TRIGGERS OFF 
GO

ALTER DATABASE [BmsManager] SET  DISABLE_BROKER 
GO

ALTER DATABASE [BmsManager] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO

ALTER DATABASE [BmsManager] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO

ALTER DATABASE [BmsManager] SET TRUSTWORTHY OFF 
GO

ALTER DATABASE [BmsManager] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO

ALTER DATABASE [BmsManager] SET PARAMETERIZATION SIMPLE 
GO

ALTER DATABASE [BmsManager] SET READ_COMMITTED_SNAPSHOT OFF 
GO

ALTER DATABASE [BmsManager] SET HONOR_BROKER_PRIORITY OFF 
GO

ALTER DATABASE [BmsManager] SET RECOVERY SIMPLE 
GO

ALTER DATABASE [BmsManager] SET  MULTI_USER 
GO

ALTER DATABASE [BmsManager] SET PAGE_VERIFY CHECKSUM  
GO

ALTER DATABASE [BmsManager] SET DB_CHAINING OFF 
GO

ALTER DATABASE [BmsManager] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO

ALTER DATABASE [BmsManager] SET TARGET_RECOVERY_TIME = 60 SECONDS 
GO

ALTER DATABASE [BmsManager] SET DELAYED_DURABILITY = DISABLED 
GO

ALTER DATABASE [BmsManager] SET ACCELERATED_DATABASE_RECOVERY = OFF  
GO

ALTER DATABASE [BmsManager] SET QUERY_STORE = OFF
GO

ALTER DATABASE [BmsManager] SET  READ_WRITE 
GO


USE [BmsManager]
GO

/****** Object:  Table [dbo].[BmsExtension]    Script Date: 2021/08/12 19:10:41 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[BmsExtension](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Extension] [nvarchar](max) NULL,
 CONSTRAINT [PK_Extension] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


USE [BmsManager]
GO

/****** Object:  Table [dbo].[RootDirectory]    Script Date: 2021/08/12 19:11:55 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[RootDirectory](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Path] [nvarchar](max) NOT NULL,
	[ParentRootID] [int] NULL,
	[FolderUpdateDate] [datetime] NOT NULL,
 CONSTRAINT [PK_Root] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


USE [BmsManager]
GO

/****** Object:  Table [dbo].[BmsFolder]    Script Date: 2021/08/12 19:12:05 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[BmsFolder](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[RootID] [int] NOT NULL,
	[Path] [nvarchar](max) NOT NULL,
	[Artist] [nvarchar](max) NULL,
	[Title] [nvarchar](max) NULL,
	[HasText] [bit] NOT NULL,
	[Preview] [nvarchar](max) NULL,
	[FolderUpdateDate] [datetime] NOT NULL,
 CONSTRAINT [PK_Folder] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


USE [BmsManager]
GO

/****** Object:  Table [dbo].[BmsFile]    Script Date: 2024/10/25 22:44:37 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[BmsFile](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[FolderID] [int] NULL,
	[Path] [nvarchar](max) NOT NULL,
	[Title] [nvarchar](max) NULL,
	[Subtitle] [nvarchar](max) NULL,
	[Genre] [nvarchar](max) NULL,
	[Artist] [nvarchar](max) NULL,
	[SubArtist] [nvarchar](max) NULL,
	[MD5] [nvarchar](max) NULL,
	[Sha256] [nvarchar](max) NOT NULL,
	[Banner] [nvarchar](max) NULL,
	[StageFile] [nvarchar](max) NULL,
	[BackBmp] [nvarchar](max) NULL,
	[Preview] [nvarchar](max) NULL,
	[PlayLevel] [nvarchar](max) NULL,
	[Mode] [int] NOT NULL,
	[Difficulty] [int] NOT NULL,
	[Judge] [int] NOT NULL,
	[MinBpm] [float] NOT NULL,
	[MaxBpm] [float] NOT NULL,
	[Length] [int] NOT NULL,
	[Notes] [int] NOT NULL,
	[Feature] [int] NOT NULL,
	[ChartHash] [nvarchar](max) NULL,
	[HasBga] [bit] NOT NULL,
	[IsNoKeySound] [bit] NOT NULL,
	[N] [int] NOT NULL,
	[LN] [int] NOT NULL,
	[S] [int] NOT NULL,
	[LS] [int] NOT NULL,
	[Total] [float] NOT NULL,
	[Density] [float] NOT NULL,
	[PeakDensity] [float] NOT NULL,
	[EndDensity] [float] NOT NULL,
	[Distribution] [nvarchar](max) NULL,
	[MainBpm] [float] NOT NULL,
	[SpeedChange] [nvarchar](max) NULL,
	[LaneNotes] [nvarchar](max) NULL,
 CONSTRAINT [PK_File] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


USE [BmsManager]
GO

/****** Object:  Table [dbo].[BmsTable]    Script Date: 2021/08/12 19:12:33 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[BmsTable](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Url] [nvarchar](max) NOT NULL,
	[Name] [nvarchar](max) NOT NULL,
	[Symbol] [nvarchar](max) NOT NULL,
	[Tag] [nvarchar](max) NULL,
 CONSTRAINT [PK_BmsTable] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


USE [BmsManager]
GO

/****** Object:  Table [dbo].[BmsTableDifficulty]    Script Date: 2021/08/12 19:12:43 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[BmsTableDifficulty](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[BmsTableID] [int] NOT NULL,
	[Difficulty] [nvarchar](max) NOT NULL,
	[DifficultyOrder] [int] NULL,
 CONSTRAINT [PK_TableDifficulty] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


USE [BmsManager]
GO

/****** Object:  Table [dbo].[BmsTableData]    Script Date: 2021/08/12 19:12:50 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[BmsTableData](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[BmsTableDifficultyID] [int] NOT NULL,
	[MD5] [nvarchar](max) NOT NULL,
	[LR2BmsID] [nvarchar](max) NULL,
	[Title] [nvarchar](max) NULL,
	[Artist] [nvarchar](max) NULL,
	[Url] [nvarchar](max) NULL,
	[DiffUrl] [nvarchar](max) NULL,
	[DiffName] [nvarchar](max) NULL,
	[PackUrl] [nvarchar](max) NULL,
	[PackName] [nvarchar](max) NULL,
	[Comment] [nvarchar](max) NULL,
	[OrgMD5] [nvarchar](max) NULL,
 CONSTRAINT [PK_TableData] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


