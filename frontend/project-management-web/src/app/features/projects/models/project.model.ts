export interface Project {
  id: string;
  code: string;
  name: string;
  description?: string;
  status: 'Planning' | 'Active' | 'OnHold' | 'Completed';
  visibility: string;
  version: number;
}

export interface ProjectMember {
  userId: string;
  username: string;
  displayName: string | null;
  role: string;
  joinedAt: string;
}
