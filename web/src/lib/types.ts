export interface PublicLink {
  platform: string;
  url: string;
  actionLabel: string;
  deezerTrackId: string | null;
  spotifyTrackId: string | null;
}

export interface PublicPage {
  id: string;
  slug: string;
  artistName: string;
  title: string;
  subtitle: string | null;
  artworkUrl: string | null;
  accentColor: string | null;
  links: PublicLink[];
}

export interface PublicPageSummary {
  slug: string;
  artistName: string;
  title: string;
  subtitle: string | null;
  artworkUrl: string | null;
  accentColor: string | null;
  linkCount: number;
}

// ----- Admin -----

export interface ResolvedLink {
  platform: string;
  url: string;
  deezerTrackId: string | null;
  spotifyTrackId: string | null;
}

export interface ResolveResult {
  artistName: string | null;
  title: string | null;
  artworkUrl: string | null;
  accentColor: string | null;
  links: ResolvedLink[];
}

export interface LinkInput {
  platform: string;
  url: string;
  actionLabel: string;
  sortOrder: number;
  isEnabled: boolean;
  deezerTrackId: string | null;
  spotifyTrackId: string | null;
}

export interface AdminPage {
  id: string;
  slug: string;
  artistName: string;
  title: string;
  subtitle: string | null;
  artworkUrl: string | null;
  accentColor: string | null;
  sourceUrl: string | null;
  isPublished: boolean;
  createdAt: string;
  updatedAt: string;
  links: LinkInput[];
}

export interface PageListItem {
  id: string;
  slug: string;
  artistName: string;
  title: string;
  artworkUrl: string | null;
  isPublished: boolean;
  totalClicks: number;
  updatedAt: string;
}

export interface PlatformCount {
  platform: string;
  clicks: number;
}

export interface DailyCount {
  date: string;
  clicks: number;
}

export interface PageStats {
  pageId: string;
  slug: string;
  totalClicks: number;
  preSaves: number;
  byPlatform: PlatformCount[];
  byDay: DailyCount[];
}
