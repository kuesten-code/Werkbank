-- Demo seed data for rapport schema
INSERT INTO rapport."TimeEntries"
    ("StartTime", "EndTime", "Description", "IsManual", "CustomerId", "CustomerName", "ProjectId", "ProjectName", "Status", "CreatedAt", "UpdatedAt")
VALUES
    ('2026-01-05T08:00:00Z', '2026-01-05T10:30:00Z', 'Kickoff meeting and planning', false, 1, 'Kuestencode GmbH', 101, 'Project Aurora', 'Stopped', NOW(), NOW()),
    ('2026-01-05T11:00:00Z', '2026-01-05T12:15:00Z', 'Implementation work', false, 2, 'Nordlicht Media', 102, 'Project Beacon', 'Stopped', NOW(), NOW()),
    ('2026-01-06T09:00:00Z', NULL, 'Currently tracking feature work', false, 1, 'Kuestencode GmbH', 101, 'Project Aurora', 'Running', NOW(), NOW()),
    ('2026-01-06T13:00:00Z', '2026-01-06T15:00:00Z', 'Manual correction for offline work', true, 3, 'Seewind IT', NULL, NULL, 'Manual', NOW(), NOW()),
    ('2026-01-07T08:30:00Z', '2026-01-07T09:45:00Z', 'Code review and cleanup', false, 2, 'Nordlicht Media', NULL, NULL, 'Stopped', NOW(), NOW());
