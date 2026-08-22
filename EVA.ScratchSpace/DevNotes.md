

generated enum
       │
       ▼
generator
       │
       ├── legal physical representation
       ├── constructors
       ├── payload accessors
       └── result type
                │
                ▼
             ref struct
                │
                ▼
          Roslyn analyzer
                │
                ├── must use
                ├── must consume
                ├── state tracking
                ├── exhaustiveness
                └── impossible payload access