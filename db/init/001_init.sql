-- Minimal DB bootstrap for Story 1.0
-- Creates one seeded user for local development.

create extension if not exists pgcrypto;

create schema if not exists app;

create table if not exists app.users (
  id uuid primary key default gen_random_uuid(),
  email text not null unique,
  display_name text not null,
  password_hash text not null,
  is_active boolean not null default true,
  created_at timestamptz not null default now()
);

insert into app.users (email, display_name, password_hash, is_active)
values ('pm1@local.test', 'PM One', 'dev-only', true)
on conflict (email) do nothing;

